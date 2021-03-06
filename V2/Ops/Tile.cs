﻿using NESSharp.Common;
using NESSharp.Core;
using System;
using System.Collections.Generic;
using System.Text;
using static NESSharp.Core.AL;

namespace NESSharp.Lib.VRamQueue.V2.Ops {
	public class Tile {
		private U8 _opTile;
		private LiveQueue _liveQueue;
		private Label _executeLoopContinue;
		public Tile(Func<Label, U8> handlerListAdd, LiveQueue queue, Label execContinue, Label _) {
			_liveQueue = queue;
			_executeLoopContinue = execContinue;
			_opTile = handlerListAdd(LabelFor(Handler));
		}
		public void DrawTile(U8 tile) {
			_liveQueue.Write(Y, () => {
				_liveQueue.Push(_opTile);
				_liveQueue.Push(tile);
			});
		}
		[CodeSection]
		private void Handler() {
			_liveQueue.Unsafe_Pop(Y);
			NES.PPU.Data.Set(_liveQueue.Unsafe_Peek(Y));
			GoTo(_executeLoopContinue);
		}
	}
}
