using NESSharp.Common;
using NESSharp.Core;
using System;
using System.Collections.Generic;
using System.Text;
using static NESSharp.Core.AL;

namespace NESSharp.Lib.VRamQueue.Ops {
	public class Pause {
		private U8 _opPause;
		private LiveQueue _liveQueue;
		private Label _executeLoopBreak;
		private VByte _pauseCount;
		public Pause(Func<Label, U8> handlerListAdd, LiveQueue queue, Label _, Label execBreak) {
			_liveQueue = queue;
			_executeLoopBreak = execBreak;
			_pauseCount = VByte.New(GlobalRam, "VRamQueue_pauseCount");
			_opPause = handlerListAdd(LabelFor(Handler));
		}
		public void For(U8 frames) {
			_liveQueue.Write(Y, () => {
				_liveQueue.Push(_opPause);
				_liveQueue.Push(frames);
			});
		}

		[CodeSection]
		private void Handler() {
			Comment("Pause");
			_liveQueue.Unsafe_Pop(Y);
			_pauseCount.Set(_liveQueue.Unsafe_Peek(Y));
			Y++;
			GoTo(_executeLoopBreak);
		}

		public void ExecuteBlockWrapper(Action block) {
			If(	Option(() => _pauseCount.Equals(0), () => {
					block();
				}),
				Default(() => {
					_pauseCount.Decrement();
					GoTo(_executeLoopBreak);
				})
			);
		}
		
		public void Reset() {
			_pauseCount.Set(0);
		}
	}
}
