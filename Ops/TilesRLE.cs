using NESSharp.Common;
using NESSharp.Core;
using System;
using System.Collections.Generic;
using System.Text;
using static NESSharp.Core.AL;

namespace NESSharp.Lib.VRamQueue.Ops {
	public class TilesRLE {
		private U8 _opTilesRLE;
		private LiveQueue _liveQueue;
		private OpLabel _executeLoopContinue;
		public TilesRLE(Func<OpLabel, U8> handlerListAdd, LiveQueue queue, OpLabel execContinue, OpLabel _) {
			_liveQueue = queue;
			_executeLoopContinue = execContinue;
			_opTilesRLE = handlerListAdd(LabelFor(Handler));
		}
		public void Draw(params U8[] tile) {
			_liveQueue.Write(Y, () => {
				_liveQueue.Push(_opTilesRLE);
				for (var i = 0; i < tile.Length; i++) { //using tile.length here so individual values can be pushed after this call
					_liveQueue.Push(tile[i]);
				}
				_liveQueue.Push(VRamQueue.Op.NOP);
			});
		}
		public void Draw_Manual() {
			_liveQueue.PushStart(Y);
			_liveQueue.Push(_opTilesRLE);
		}
		public void Draw_Manual_Done() {
			_liveQueue.Push(VRamQueue.Op.NOP);
		}
		[CodeSection]
		private void Handler() {
			_liveQueue.Unsafe_Pop(Y);
			X.Set(_liveQueue.Unsafe_Peek(Y)); //Number of bytes of data
			Loop.While(() => X.NotEquals(VRamQueue.Op.NOP), () => {
				_liveQueue.Unsafe_Pop(Y);
				A.Set(_liveQueue.Unsafe_Peek(Y));
				Loop.Descend(X, () => {
					NES.PPU.Data.Set(A);
				});
				_liveQueue.Unsafe_Pop(Y);
				X.Set(_liveQueue.Unsafe_Peek(Y));
			});
			GoTo(_executeLoopContinue);
		}
	}
}
