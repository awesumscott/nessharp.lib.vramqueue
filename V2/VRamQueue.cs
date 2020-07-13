using NESSharp.Common;
using NESSharp.Core;
using NESSharp.Lib.VRamQueue.V2.Ops;
using System;
using System.Collections.Generic;
using System.Linq;
using static NESSharp.Core.AL;

namespace NESSharp.Lib.VRamQueue.V2 {
	//public interface IVRamQueueHandlers {
	//	void Add(Action codeSection);
	//	void Break();
	//	void Continue();
	//	void Done();
	//}
	public abstract class VRamQueueOp {
		public VRamQueue Queue;
		public U8 Id;
		public void Init(VRamQueue queue, U8 id) {
			Queue = queue;
			Id = id;
		}
		public abstract void AddHandlers();
	}
	public class Pause : VRamQueueOp {
		public override void AddHandlers() => Queue.Add(Handler);
		[CodeSection]
		public void Handler() {
			Queue.Continue();
		}

		public void Reset() {

		}
		
		public void ExecuteBlockWrapper(Action block) {
		
		}
	}
	/// <summary>
	/// Routines for queuing PPU draws to be executed during VBlank.
	/// </summary>
	/// <remarks>
	/// 1. Call Init() directly within ROM layout definition class, before any module uses the queue
	/// 2. Include.Module(VRamQueue) in bank X
	/// 3. Call Execute() in bank X, and make sure it gets run within VBlank
	/// 4. Call queuing helpers directly from any other bank and module--no bankswitching necessary
	/// </remarks>
	public class VRamQueue : Module {
		private List<VRamQueueOp> _ops;
		private VByte _done;
		public LiveQueue _liveQueue;
		private Label _executeLoopContinue;
		private Label _executeLoopBreak;
		
		private U8 _optionId = 0;
		private List<Label> _opHandlers;
		private LabelList HandlerList;

		public T? Op<T>() where T : VRamQueueOp, new() {
			return (T?)_ops.Where(x => x is T).FirstOrDefault();
		}
		public VRamQueue AddOps(params VRamQueueOp[] ops) {
			//TODO: exception if module is already loaded
			_ops.AddRange(ops);
			return this;
		}

		[Dependencies]
		public void Dependencies() {
			foreach (var op in _ops)
				op.Init(this, _optionId++);
			HandlerList = new LabelList(_opHandlers.ToArray());
		}
		[DataSection]
		public void HandlerAddresses() => HandlerList.WriteList();
		
		//TODO: this doesn't have to be aligned to a page, so allow scenes to use this directly with their ram refs
		public void Setup(U16 pageStart, U8 length) {
			_done = VByte.New(Ram, $"{nameof(VRamQueue)}{nameof(_done)}");
			_executeLoopContinue = Labels.New();
			_executeLoopBreak = Labels.New();

			var VRAM = Ram.Allocate(Addr(pageStart), Addr((U16)(pageStart + 0xFF)));
			_ops = new List<VRamQueueOp>();
			_ops.Add(new NOP());
			_opHandlers = new List<Label>();
			_liveQueue = LiveQueue.New(Zp, Ram, VRAM, length, $"{nameof(VRamQueue)}{nameof(_liveQueue)}", Op<Stop>()?.Id ?? 255);
		}
		private U8 AddHandler(Label handlerLabel) {
			_opHandlers.Add(handlerLabel);
			return _optionId++;
		}
		public void Reset() {
			Op<Pause>()?.Reset();
			_done.Set(1);
			_liveQueue.Reset();
		}
		public void Push(U8 val) {
			_liveQueue.Push(val);
		}
		public void Push(VByte val) {
			_liveQueue.Push(val);
		}
		public void Push(Func<RegisterA> val) {
			_liveQueue.Push(val.Invoke());
		}
		public void DonePushing() {
			_liveQueue.PushDone();
		}
		//TODO: remove this and use the op directly
		public void EndFrame() {
			_liveQueue.PushOnce(Y, Op<EOF>().Id);
		}

		public Condition IsDone() =>		_done.NotEquals(0);
		public Condition IsNotDone() =>		_done.Equals(0);
		public Condition IsEmpty() =>		_liveQueue.Empty();
		public Condition IsNotEmpty() =>	_liveQueue.NotEmpty();

		public void Execute() {
			Action loopBody = () => {
				_done.Set(0);

				Comment("Use current Op to find the op handler address, then indirect JMP");
				HandlerList.GoTo(X.Set(_liveQueue.Peek()));

				Use(_executeLoopContinue);
				_liveQueue.Pop();
			};

			var pause = Op<Pause>();
			_liveQueue.Read(Y, () => {
				Loop.Infinite(_ => {
					if (pause != null) {
						pause.ExecuteBlockWrapper(loopBody);
					} else {
						loopBody();
					}
				});
				Use(_executeLoopBreak);
			});
		}
		
		[CodeSection]
		private void Handler_NOP() {
			GoTo(_executeLoopContinue);
		}

		public void Add(Action codeSection) {
			_opHandlers.Add(LabelFor(codeSection));
		}

		public void Break() => GoTo(_executeLoopBreak);
		public void Continue() => GoTo(_executeLoopContinue);
		public void Done() => _done.Set(1);
		public void Unsafe_Pop() => _liveQueue.Unsafe_Pop(Y);
	}
}
