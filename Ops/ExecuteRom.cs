using NESSharp.Common;
using NESSharp.Core;
using System;
using static NESSharp.Core.AL;

namespace NESSharp.Lib.VRamQueue.Ops {
	public class ExecuteRom {
		private U8 _opExecuteRom;
		private LiveQueue _liveQueue;
		private Label _executeLoopContinue;
		private Ptr _ptrRomStart;
		public ExecuteRom(Func<Label, U8> handlerListAdd, LiveQueue queue, Label execContinue, Label _) {
			_liveQueue = queue;
			_executeLoopContinue = execContinue;
			_opExecuteRom = handlerListAdd(LabelFor(Handler));
			_ptrRomStart = Ptr.New(GlobalZp, "VRamQueue_ExecuteRom_ptrRomStart");
		}
		public void Write(Core.Address ramStart, U8 len) {
			_liveQueue.Write(Y, () => {
				_liveQueue.Push(_opExecuteRom);
				_liveQueue.Push(ramStart.Lo);
				_liveQueue.Push(ramStart.Hi);
				_liveQueue.Push(len);
			});
		}
		public void Write(Label lbl, U8 len) {
			_liveQueue.Write(Y, () => {
				_liveQueue.Push(_opExecuteRom);
				_liveQueue.Push(lbl.Lo());
				_liveQueue.Push(lbl.Hi());
			});
		}
		public void WriteROM(Label lbl, U8 len) {
			Raw(_opExecuteRom);
			Raw(lbl.Lo());
			Raw(lbl.Hi());
		}
		[CodeSection]
		private void Handler() {
			_liveQueue.Unsafe_Pop(Y);
			_ptrRomStart.Lo.Set(_liveQueue.Unsafe_Peek(Y));
			_liveQueue.Unsafe_Pop(Y);
			_ptrRomStart.Hi.Set(_liveQueue.Unsafe_Peek(Y));
			_liveQueue.Unsafe_Pop(Y);
			A.Set(_liveQueue.Unsafe_Peek(Y)); //Number of bytes of data

			Loop.While_Pre(() => A.NotEquals(VRamQueue.Op.NOP), _ => { });

			Stack.Preserve(Y, () => {
				Y.Set(0);
				Loop.Descend_Post(X, _ => {
					NES.PPU.Data.Set(TempPtr0[Y]);
					Y++;
				});
			});

			GoTo(_executeLoopContinue);
		}
	}
}
