using NESSharp.Core;
using static NESSharp.Core.AL;

namespace NESSharp.Lib.VRamQueue.V2.Ops {
	public class EOF : VRamQueueOp {
		public override void AddHandlers() => Queue.Add(Handler);
		[CodeSection]
		private void Handler() {
			Comment("End of frame");
			Queue.Unsafe_Pop();
			Queue.Break();
		}
	}
}
