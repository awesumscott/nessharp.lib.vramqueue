using NESSharp.Core;

namespace NESSharp.Lib.VRamQueue.V2.Ops {
	public class NOP : VRamQueueOp {
		public override void AddHandlers() => Queue.Add(Handler);
		[CodeSection]
		public void Handler() {
			Queue.Continue();
		}
	}
}
