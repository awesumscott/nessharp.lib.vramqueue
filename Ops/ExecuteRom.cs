using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using NESSharp.Common;
using NESSharp.Core;
using static NESSharp.Core.AL;

namespace NESSharp.Lib.VRamQueue.Ops {
	public class ExecuteRom {
		private U8 _opExecuteRom;
		private LiveQueue _liveQueue;
		private OpLabel _executeLoopContinue;
		private Ptr _ptrRomStart;
		public ExecuteRom(Func<OpLabel, U8> handlerListAdd, LiveQueue queue, OpLabel execContinue, OpLabel _) {
			_liveQueue = queue;
			_executeLoopContinue = execContinue;
			_opExecuteRom = handlerListAdd(LabelFor(Handler));
			_ptrRomStart = Ptr.New("VRamQueue_ExecuteRom_ptrRomStart");
		}
		public void Write(Core.Address ramStart, U8 len) {
			_liveQueue.Write(Y, () => {
				_liveQueue.Push(_opExecuteRom);
				_liveQueue.Push(ramStart.Lo);
				_liveQueue.Push(ramStart.Hi);
				_liveQueue.Push(len);
			});
		}
		public void Write(OpLabel lbl, U8 len) {
			_liveQueue.Write(Y, () => {
				_liveQueue.Push(_opExecuteRom);
				_liveQueue.Push(lbl.Lo());
				_liveQueue.Push(lbl.Hi());
			});
		}
		public void WriteROM(OpLabel lbl, U8 len) {
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

			Loop.While(() => A.NotEquals(VRamQueue.Op.NOP), () => {
				
			});
			

			Stack.Preserve(Y, () => {
				Y.Set(0);
				Loop.Descend(X, () => {
					NES.PPU.Data.Set(TempPtr0[Y]);
					Y++;
				});
			});

			GoTo(_executeLoopContinue);
		}
	}
}
