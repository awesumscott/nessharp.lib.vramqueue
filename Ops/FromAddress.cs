using System;
using System.Collections.Generic;
using System.Text;
using NESSharp.Common;
using NESSharp.Core;
using static NESSharp.Core.AL;

namespace NESSharp.Lib.VRamQueue.Ops {
	public class FromAddress {
		private U8 _opFromAddress;
		private LiveQueue _liveQueue;
		private OpLabel _executeLoopContinue;
		public FromAddress(Func<OpLabel, U8> handlerListAdd, LiveQueue queue, OpLabel execContinue, OpLabel _) {
			_liveQueue = queue;
			_executeLoopContinue = execContinue;
			_opFromAddress = handlerListAdd(LabelFor(Handler));
		}
		public void Write(Core.Address ramStart, U8 len) {
			_liveQueue.Write(Y, () => {
				_liveQueue.Push(_opFromAddress);
				_liveQueue.Push(ramStart.Lo);
				_liveQueue.Push(ramStart.Hi);
				_liveQueue.Push(len);
			});
		}
		public void Write(OpLabel lbl, U8 len) {
			_liveQueue.Write(Y, () => {
				_liveQueue.Push(_opFromAddress);
				_liveQueue.Push(lbl.Lo());
				_liveQueue.Push(lbl.Hi());
				_liveQueue.Push(len);
			});
		}
		[CodeSection]
		private void Handler() {
			_liveQueue.Unsafe_Pop(Y);
			TempPtr0.Lo.Set(_liveQueue.Unsafe_Peek(Y));
			_liveQueue.Unsafe_Pop(Y);
			TempPtr0.Hi.Set(_liveQueue.Unsafe_Peek(Y));
			_liveQueue.Unsafe_Pop(Y);
			X.Set(_liveQueue.Unsafe_Peek(Y)); //Number of bytes of data

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
