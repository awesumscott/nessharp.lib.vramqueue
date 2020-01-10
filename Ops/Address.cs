using NESSharp.Common;
using NESSharp.Core;
using System;
using System.Collections.Generic;
using System.Text;
using static NESSharp.Core.AL;

namespace NESSharp.Lib.VRamQueue.Ops {
	public class Address {
		private U8 _opAddr;
		private LiveQueue _liveQueue;
		private OpLabel _executeLoopContinue;
		public Address(Func<OpLabel, U8> handlerListAdd, LiveQueue queue, OpLabel execContinue, OpLabel _) {
			_liveQueue = queue;
			_executeLoopContinue = execContinue;
			_opAddr = handlerListAdd(LabelFor(Handler));
		}
		public void Set(U16 addr) {
			_liveQueue.Write(Y, () => {
				_liveQueue.Push(_opAddr);
				_liveQueue.Push(addr.Hi);
				_liveQueue.Push(addr.Lo);
			});
		}
		public void Set(Var16 addr) {
			_liveQueue.Write(Y, () => {
				_liveQueue.Push(_opAddr);
				_liveQueue.Push(addr.Hi);
				_liveQueue.Push(addr.Lo);
			});
		}
		[CodeSection]
		private void Handler() {
			_liveQueue.Unsafe_Pop(Y);
			NES.PPU.Reset();
			NES.PPU.Address.Set(_liveQueue.Unsafe_Peek(Y));
			_liveQueue.Unsafe_Pop(Y);
			NES.PPU.Address.Set(_liveQueue.Unsafe_Peek(Y));
			GoTo(_executeLoopContinue);
		}
	}
}
