using NESSharp.Common;
using NESSharp.Core;
using System;
using static NESSharp.Core.AL;

namespace NESSharp.Lib.VRamQueue.V2.Ops {
	public class Address : VRamQueueOp {
		private LiveQueue _liveQueue;
		public override void AddHandlers() => Queue.Add(Handler);
		public void Set(U16 addr) {
			Queue._liveQueue.Write(Y, () => {
				Queue._liveQueue.Push(Id);
				Queue._liveQueue.Push(addr.Hi);
				Queue._liveQueue.Push(addr.Lo);
			});
		}
		public void Set(VarN n) {
			if (n.Size != 2) throw new Exception("Value must be 2 bytes");
			Queue._liveQueue.Write(Y, () => {
				Queue._liveQueue.Push(Id);
				Queue._liveQueue.Push(n.Address[1]);
				Queue._liveQueue.Push(n.Address[0]);
			});
		}

		public void SetROM(U16 addr) {
			Raw(Id);
			Raw(addr.Lo, addr.Hi);
		}

		[CodeSection]
		private void Handler() {
			Queue._liveQueue.Unsafe_Pop(Y);
			NES.PPU.Reset();
			NES.PPU.Address.Set(Queue._liveQueue.Unsafe_Peek(Y));
			Queue._liveQueue.Unsafe_Pop(Y);
			NES.PPU.Address.Set(Queue._liveQueue.Unsafe_Peek(Y));
			Queue.Continue();
		}
	}
}
