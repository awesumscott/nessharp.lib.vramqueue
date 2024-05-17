using NESSharp.Common;
using NESSharp.Core;
using System;
using static NESSharp.Core.CPU6502;

namespace NESSharp.Lib.VRamQueue.Ops {
	public class Pause {
		private readonly U8 _opPause;
		private readonly LiveQueue _liveQueue;
		private readonly Label _executeLoopBreak;
		private readonly VByte _pauseCount;
		public Pause(Func<Label, U8> handlerListAdd, LiveQueue queue, Label _, Label execBreak) {
			_liveQueue = queue;
			_executeLoopBreak = execBreak;
			_pauseCount = VByte.New(NES.ram, "VRamQueue_pauseCount");
			_opPause = handlerListAdd(AL.LabelFor(Handler));
		}
		public void For(U8 frames) {
			_liveQueue.Write(Y, () => {
				_liveQueue.Push(_opPause);
				_liveQueue.Push(frames);
			});
		}

		[CodeSection]
		private void Handler() {
			AL.Comment("Pause");
			_liveQueue.Unsafe_Pop(Y);
			_pauseCount.Set(_liveQueue.Unsafe_Peek(Y));
			Y.Inc();
			AL.GoTo(_executeLoopBreak);
		}

		public void ExecuteBlockWrapper(Action block) {
			If.Block(c => c
				.True(() => _pauseCount.Equals(0), block)
				.Else(() => {
					_pauseCount.Dec();
					AL.GoTo(_executeLoopBreak);
				})
			);
		}

		public void Reset() => _pauseCount.Set(0);
	}
}
