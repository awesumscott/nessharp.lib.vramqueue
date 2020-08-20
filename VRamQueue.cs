using NESSharp.Common;
using NESSharp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using static NESSharp.Core.AL;

namespace NESSharp.Lib.VRamQueue {
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
		private LiveQueue _liveQueue;
		private Label _executeLoopContinue;
		private Label _executeLoopBreak;
		//private static VWord _handlerAddress;
		private Option[] _options;
		
		public Ops.Address Address;
		public Ops.Increment Increment;
		public Ops.Pause Pause;
		public Ops.Tile Tile;
		public Ops.TileArray TileArray;
		public Ops.TilesRLE TilesRLE;
		public Ops.FromAddress FromAddress;
		public Ops.Palettes Palettes;
		private U8 _optionId = 0;
		private LabelList HandlerList;

		//TODO: this doesn't have to be aligned to a page, so allow scenes to use this directly with their ram refs
		public void Setup(U16 pageStart, U8 length, params Option[] options) {
			_done = VByte.New(Ram, $"{nameof(VRamQueue)}{nameof(_done)}");
			//_handlerAddress = VWord.New(ram, "VRamQueue_handlerAddress");
			_executeLoopContinue = Labels.New();
			_executeLoopBreak = Labels.New();

			var VRAM = Ram.Allocate(Addr(pageStart), Addr((U16)(pageStart + 0xFF)));
			_liveQueue = LiveQueue.New(Zp, Ram, VRAM, length, $"{nameof(VRamQueue)}{nameof(_liveQueue)}", Op.Stop);

			_options = options ?? new Option[0];

			OptionModules();
			
		}
		private U8 AddHandler(Label handlerLabel) {
			_opHandlers.Add(handlerLabel);
			return _optionId++;
		}
		//[Dependencies]
		public void OptionModules() {
			_opHandlers = new List<Label>(){
				LabelFor(Handler_NOP),
				LabelFor(Handler_Stop),
				LabelFor(Handler_EOF),
			};
			_optionId = (U8)(_opHandlers.Count);
			if (_options.Contains(Option.Addr)) {
				Address = new Ops.Address(AddHandler, _liveQueue, _executeLoopContinue, _executeLoopBreak);
				Include.Module(Address);
			}
			if (_options.Contains(Option.Increment)) {
				Increment = new Ops.Increment(AddHandler, _liveQueue, _executeLoopContinue, _executeLoopBreak);
				Include.Module(Increment);
			}
			if (_options.Contains(Option.Pause)) {
				Pause = new Ops.Pause(AddHandler, _liveQueue, _executeLoopContinue, _executeLoopBreak);
				Include.Module(Pause);
			}
			if (_options.Contains(Option.Tile)) {
				Tile = new Ops.Tile(AddHandler, _liveQueue, _executeLoopContinue, _executeLoopBreak);
				Include.Module(Tile);
			}
			if (_options.Contains(Option.TileArray)) {
				TileArray = new Ops.TileArray(AddHandler, _liveQueue, _executeLoopContinue, _executeLoopBreak);
				Include.Module(TileArray);
			}
			if (_options.Contains(Option.TilesRLE)) {
				TilesRLE = new Ops.TilesRLE(AddHandler, _liveQueue, _executeLoopContinue, _executeLoopBreak);
				Include.Module(TilesRLE);
			}
			if (_options.Contains(Option.FromAddress)) {
				FromAddress = new Ops.FromAddress(AddHandler, _liveQueue, _executeLoopContinue, _executeLoopBreak);
				Include.Module(FromAddress);
			}
			if (_options.Contains(Option.Palettes)) {
				Palettes = new Ops.Palettes(AddHandler, _liveQueue, _executeLoopContinue, _executeLoopBreak);
				Include.Module(Palettes);
			}

			HandlerList = new LabelList(_opHandlers.ToArray());
		}

		public enum Option {
			Addr,			//Set PPU Address (U16 addr)
			Increment,		//Options for incrementing PPU address writes
			Tile,			//Write Data (U8 tile)
			TileArray,		//Write Sequence of Data (U8 len, U8[] tiles)
			TilesRLE,		//Write Sequence of Data (U8 len, U8[] RLEdata)
			FromAddress,	//Write Sequence of Data (U8 len, Addr ramStart)
			Palettes,
			//DrawTilesROM,	//Write Sequence of Data (U8 len, Addr romStart)
			Pause,			//End of frame (U8 frames) -- stop parsing for a specified # of frames
		};

		public static class Op {
			public static readonly U8 NOP			= 0x00;
			public static readonly U8 Stop			= 0x01;		//Stop Here -- Decrement index so execution will wait here until it is cleared and new commands are added to queue
			public static readonly U8 EOF			= 0x02;		//End of frame -- increment and resume parsing next frame
		}
		private List<Label> _opHandlers;
		public void Reset() {
			if (_options.Contains(Option.Pause))
				Pause.Reset();
			_done.Set(1);
			_liveQueue.Reset();
		}
		public void Push(U8 val) {
			_liveQueue.Push(val);
		}
		public void Push(IOperand val) {
			_liveQueue.Push(val);
		}
		public void Push(Func<RegisterA> val) {
			_liveQueue.Push(val.Invoke());
		}
		public void DonePushing() {
			_liveQueue.PushDone();
		}
		public void EndFrame() {
			_liveQueue.PushOnce(Y, Op.EOF);
		}
		public void EndFrameROM() {
			Raw(Op.EOF);
		}
		public void NOPROM() {
			Raw(Op.NOP);
		}

		public Condition Done() =>		_done.NotEquals(0);
		public Condition NotDone() =>	_done.Equals(0);
		public Condition Empty() =>		_liveQueue.Empty();
		public Condition NotEmpty() =>	_liveQueue.NotEmpty();

		public void Execute() {
			Action loopBody = () => {
				_done.Set(0);

				Comment("Use current Op to find the op handler address, then indirect JMP");
				HandlerList.GoTo(X.Set(_liveQueue.Peek()));

				Use(_executeLoopContinue);
				_liveQueue.Pop();
			};

			_liveQueue.Read(Y, () => {
				Loop.Infinite(_ => {
					if (_options.Contains(Option.Pause)) {
						Pause.ExecuteBlockWrapper(loopBody);
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
		[CodeSection]
		private void Handler_Stop() {
			//Skip incrementing Y, so next read takes place at the same index, and rely on Stop being overwritten
			Comment("Stop updates until more data is queued");
			_done.Set(1);
			GoTo(_executeLoopBreak);
		}
		[CodeSection]
		private void Handler_EOF() {
			Comment("End of frame");
			_liveQueue.Unsafe_Pop(Y);
			GoTo(_executeLoopBreak);
		}
		[DataSection]
		public void HandlerAddresses() => HandlerList.WriteList();
		
		public void CopyBlock(Action action) {
			_liveQueue.PushRangeOnce(action);
		}
		//public void CopyBlock(Action block, VByte len) {
		//	//make sure _liveQueue.Values.Index is actually in the register before using this
		//	using (var stream = new Stream(block, len)) {
		//		stream.CopyTo(_liveQueue.Values[Y]);
		//	}
		//	//TODO: increase livequeue's index by len
		//}
	}

	public class Stream : IDisposable {
		private Ptr _ptrSrc = TempPtr0;
		private Ptr _ptrDest = TempPtr1;
		private VByte _len;
		public Stream(Action action, VByte len) {
			_ptrSrc.PointTo(LabelFor(action));
			_len = len;
		}
		public Stream CopyTo(Ptr dest) { //, RegisterX x) {
			_ptrDest.PointTo(dest);
			Loop.AscendWhile(Y.Set(0), () => Y.NotEquals(_len), _ => {
				_ptrDest[Y].Set(A.Set(_ptrSrc[Y]));
			});
			return this;
		}
		public Stream CopyTo(VByte dest) { //, RegisterX x) {
			_ptrDest.PointTo(dest);
			Loop.AscendWhile(Y.Set(0), () => Y.NotEquals(_len), _ => {
				_ptrDest[Y].Set(A.Set(_ptrSrc[Y]));
			});
			return this;
		}

		public void Dispose() {}
	}
}
