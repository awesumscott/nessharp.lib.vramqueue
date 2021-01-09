﻿using NESSharp.Common;
using NESSharp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static NESSharp.Core.AL;

namespace NESSharp.Lib.VRamQueue.Ops {
	public class TilesRLE {
		private U8 _opTilesRLE;
		private LiveQueue _liveQueue;
		private Label _executeLoopContinue;
		public TilesRLE(Func<Label, U8> handlerListAdd, LiveQueue queue, Label execContinue, Label _) {
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
		public void DrawROM(params U8[] tile) {
			Raw(_opTilesRLE);
			Raw(tile.Select(x => (byte)x).ToArray());
			Raw(VRamQueue.Op.NOP);
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
			Loop.While_Pre(() => X.NotEquals(VRamQueue.Op.NOP), _ => {
				_liveQueue.Unsafe_Pop(Y);
				A.Set(_liveQueue.Unsafe_Peek(Y));
				Loop.Descend_Post(X, _ => {
					NES.PPU.Data.Set(A);
				});
				_liveQueue.Unsafe_Pop(Y);
				X.Set(_liveQueue.Unsafe_Peek(Y));
			});
			GoTo(_executeLoopContinue);
		}
	}
}
