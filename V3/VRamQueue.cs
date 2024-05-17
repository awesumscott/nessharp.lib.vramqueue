using NESSharp.Core;
using System;
using System.Collections.Generic;
using static NESSharp.Core.CPU6502;

namespace NESSharp.Lib.VRamQueue.V3;
/*
	Ideas:

	-If the length is a multiple of 2, each cmd can just be a JMP address
		-Byte pairs starting with 00 can signal a special command like EOF or STOP
		-Since entries are always in pairs, there won't be a need to check if write index wraps twice, just once at the end

*/
public class LiveQueue2 {
	public class LiveQueue2Indexed {
		private LiveQueue2 _liveQueue;
		private IndexingRegister _indexReg;
		public LiveQueue2Indexed(LiveQueue2 lq, IndexingRegister r) {
			_liveQueue = lq;
			_indexReg = r;
		}
		public void Push(IOperand o) {
			_indexReg.Set(_liveQueue.WriteIndex);
			_liveQueue.Values[_indexReg].Set(o);
			_liveQueue._incrementWrite();
		}
		public void Push(U8 v) => Push((IOperand)v);
		public void Push(Label lbl) {
			//Push(lbl.Hi());
			//Push(lbl.Lo());
			_pushTwoBytes(lbl.Hi(), lbl.Lo());
		}
		public void Push(Action action) => Push(AL.LabelFor(action));
		public void Push(Address addr) {
			//Push(addr.Hi);
			//Push(addr.Lo);
			_pushTwoBytes(addr.Hi, addr.Lo);
		}
		private void _pushTwoBytes(IOperand o0, IOperand o1) {
			_indexReg.Set(_liveQueue.WriteIndex);
			_liveQueue.Values[_indexReg].Set(o0);
			_indexReg.Inc();
			_liveQueue.Values[_indexReg].Set(o1);
			_liveQueue.WriteIndex.Set(_indexReg);
			_liveQueue._clampWriteIndex();
		}
	}
	public LiveQueue2Indexed this[IndexingRegister r] => new(this, r);

	public Array<VByte> Values;
	private U8 _stopVal = 0;
	public VByte WriteIndex;
	public VByte ReadIndex;
	//private bool _isReading = false, _isWriting = false;
	private IndexingRegister _indexReg = null;
	public LiveQueue2() { }

	//TODO: this doesn't really enforce length. Implement wrap if length != 0 (0 would indicate 256--full page)
	public static LiveQueue2 New(RAM Ram, RAMRange valuesRam, int length, string name, U8 stopVal) {
		var bq = new LiveQueue2();
		bq.Values = Array<VByte>.New(length, valuesRam, name + "_values");
		bq._stopVal = stopVal;
		bq.WriteIndex = VByte.New(Ram.Zp, name + "_write");
		bq.ReadIndex = VByte.New(Ram.Zp, name + "_read");
		return bq;
	}
	public Condition Empty() => ReadIndex.Equals(WriteIndex);
	public Condition NotEmpty() => ReadIndex.NotEquals(WriteIndex);

	private void _incrementRead() {
		ReadIndex.Inc();
		_clampReadIndex();
	}
	private void _incrementWrite() {
		WriteIndex.Inc();
		_clampWriteIndex();
	}
	private void _clampReadIndex() {
		If.True(() => ReadIndex.Equals(Values.Length), () => ReadIndex.Set(0));
	}
	private void _clampWriteIndex() {
		If.True(() => WriteIndex.Equals(Values.Length), () => WriteIndex.Set(0));
	}

	public void Reset() {
		WriteIndex.Set(0);
		ReadIndex.Set(0);
		Values[0].Set(_stopVal);
	}
	//public VByte Peek() {
	//	if (!_isReading)
	//		throw new Exception("Peek can only be used within a LiveQueue.Read() block");
	//	return Values[_indexReg];
	//}
	//public void Pop() {
	//	if (!_isReading)
	//		throw new Exception("Pop can only be used within a LiveQueue.Read() block");
	//	_indexReg.Inc();
	//}
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
	private VByte _done;
	private LiveQueue2 _liveQueue;
	private Label _executeLoopContinue;
	private Label _executeLoopBreak;
	private Common.LabelList HandlerList;

	////TODO: this doesn't have to be aligned to a page, so allow scenes to use this directly with their ram refs
	//public void SetupAligned(Address pageStart, U8 length, params Option[] options) {
	//	_done = VByte.New(Ram, $"{nameof(VRamQueue)}{nameof(_done)}");
	//	//_handlerAddress = VWord.New(ram, "VRamQueue_handlerAddress");
	//	_executeLoopContinue = Labels.New();
	//	_executeLoopBreak = Labels.New();

	//	var VRAM = Ram.Allocate(pageStart, Addr((U16)(pageStart + 0xFF)), "VRAM");
	//	_liveQueue = LiveQueue.New(Zp, Ram, VRAM, length, $"{nameof(VRamQueue)}{nameof(_liveQueue)}", Op.Stop);

	//	OptionModules();
	//}
	public void Setup(int length) {//, params Module[] modules) {
		_done = VByte.New(Ram, $"{nameof(VRamQueue)}{nameof(_done)}");
		//_handlerAddress = VWord.New(ram, "VRamQueue_handlerAddress");
		_executeLoopContinue = AL.Labels.New();
		_executeLoopBreak = AL.Labels.New();

		_liveQueue = LiveQueue2.New(NES.Mem, Ram, length, $"{nameof(VRamQueue)}{nameof(_liveQueue)}", Op.Stop);

		_opHandlers = new List<Label>(){
			AL.LabelFor(Handler_NOP),
			AL.LabelFor(Handler_Stop),
			AL.LabelFor(Handler_EOF),
		};

		HandlerList = new Common.LabelList(_opHandlers.ToArray());







		////TEST ONLY
		//var lq2 = new LiveQueue2();
		//lq2[X].Push(DonePushing);
	}


	//private U8 AddHandler(Label handlerLabel) {
	//	_opHandlers.Add(handlerLabel);
	//	return _optionId++;
	//}

	public static class Op {
		public static readonly U8 NOP			= 0x00;
		public static readonly U8 Stop			= 0x01;		//Stop Here -- Decrement index so execution will wait here until it is cleared and new commands are added to queue
		public static readonly U8 EOF			= 0x02;		//End of frame -- increment and resume parsing next frame
	}
	private List<Label> _opHandlers;
	public void Reset() {
		_done.Set(1);
		_liveQueue.Reset();
	}
	public void Push(U8 val) {
		_liveQueue[Y].Push(val);
	}
	public void Push(IOperand val) {
		_liveQueue[Y].Push(val);
	}
	public void Push(Func<RegisterA> val) {
		_liveQueue[Y].Push(val.Invoke());
	}
	//public void DonePushing() {
	//	_liveQueue[Y].PushDone();
	//}
	//public void EndFrame() {
	//	_liveQueue[Y].PushOnce(Y, Op.EOF);
	//}

	public Condition Done() =>		_done.NotEquals(0);
	public Condition NotDone() =>	_done.Equals(0);
	public Condition Empty() =>		_liveQueue.Empty();
	public Condition NotEmpty() =>	_liveQueue.NotEmpty();

	public void Execute() {
		Loop.Infinite(_ => {
			_done.Set(0);

			//Comment("Use current Op to find the op handler address, then indirect JMP");
			//HandlerList.GoTo(X.Set(_liveQueue.Peek()));

			Y.Set(_liveQueue.ReadIndex);
			If.Block(z => z
				.True(() => Y.Equals(0), () => {
					
				})
				.Else(() => {
					AL.Comment("Byte pair is a JMP address");
					Stack.Backup(Y);
					Y.Inc();
					Stack.Backup(Y);

				})
			);


			_executeLoopContinue.Write();
			//_liveQueue.Pop();
		});
		_executeLoopBreak.Write();
	}
	
	[CodeSection]
	private void Handler_NOP() {
		AL.GoTo(_executeLoopContinue);
	}
	[CodeSection]
	private void Handler_Stop() {
		//Skip incrementing Y, so next read takes place at the same index, and rely on Stop being overwritten
		AL.Comment("Stop updates until more data is queued");
		_done.Set(1);
		AL.GoTo(_executeLoopBreak);
	}
	[CodeSection]
	private void Handler_EOF() {
		AL.Comment("End of frame");
		//_liveQueue.Unsafe_Pop(Y);
		AL.GoTo(_executeLoopBreak);
	}
	[DataSection]
	public void HandlerAddresses() => HandlerList.WriteList();
	
	public void CopyBlock(Action action) {
		//_liveQueue.PushRangeOnce(action);
	}
}
