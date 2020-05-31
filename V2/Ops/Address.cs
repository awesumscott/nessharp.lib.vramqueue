using NESSharp.Common;
using NESSharp.Core;
using System;
using static NESSharp.Core.AL;

namespace NESSharp.Lib.VRamQueue.V2.Ops {
	public class Address : VRamQueueOp {
		private LiveQueue _liveQueue;
		public Address(LiveQueue queue) {
			_liveQueue = queue;
		}
		public override void AddHandlers() => Queue.Add(Handler);
		public void Set(U16 addr) {
			_liveQueue.Write(Y, () => {
				_liveQueue.Push(Id);
				_liveQueue.Push(addr.Hi);
				_liveQueue.Push(addr.Lo);
			});
		}
		public void Set(VarN n) {
			if (n.Size != 2) throw new Exception("Value must be 2 bytes");
			_liveQueue.Write(Y, () => {
				_liveQueue.Push(Id);
				_liveQueue.Push(n.Address[1]);
				_liveQueue.Push(n.Address[0]);
			});
		}

		public void SetROM(U16 addr) {
			Raw(Id);
			Raw(addr.Lo, addr.Hi);
		}

		[CodeSection]
		private void Handler() {
			_liveQueue.Unsafe_Pop(Y);
			NES.PPU.Reset();
			NES.PPU.Address.Set(_liveQueue.Unsafe_Peek(Y));
			_liveQueue.Unsafe_Pop(Y);
			NES.PPU.Address.Set(_liveQueue.Unsafe_Peek(Y));
			Queue.Continue();
		}
	}
}
