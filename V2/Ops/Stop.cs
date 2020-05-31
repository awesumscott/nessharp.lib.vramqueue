using NESSharp.Core;
using static NESSharp.Core.AL;

namespace NESSharp.Lib.VRamQueue.V2.Ops {
	public class Stop : VRamQueueOp {
		public override void AddHandlers() => Queue.Add(Handler);
		[CodeSection]
		public void Handler() {
			//Skip incrementing Y, so next read takes place at the same index, and rely on Stop being overwritten
			Comment("Stop updates until more data is queued");
			Queue.Done();
			Queue.Break();
		}
	}
}
