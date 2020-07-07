using NESSharp.Common;
using NESSharp.Core;
using System;
using System.Collections.Generic;
using System.Text;
using static NESSharp.Core.AL;

namespace NESSharp.Lib.VRamQueue.V2.Ops {
	public class TileArray {
		private U8 _opTileArray;
		private LiveQueue _liveQueue;
		private Label _executeLoopContinue;
		public TileArray(Func<Label, U8> handlerListAdd, LiveQueue queue, Label execContinue, Label _) {
			_liveQueue = queue;
			_executeLoopContinue = execContinue;
			_opTileArray = handlerListAdd(LabelFor(Handler));
		}
		public void Draw(params U8[] tile) {
			_liveQueue.Write(Y, () => {
				_liveQueue.Push(_opTileArray);
				_liveQueue.Push((U8)tile.Length);
				for (var i = 0; i < tile.Length; i++) { //using tile.length here so individual values can be pushed after this call
					_liveQueue.Push(tile[i]);
				}
			});

			//TODO: drawing tiles in a sequence can be done with a write() inline func, adding one at a time after
			//setting length already when the length is already known. If it isn't known, maybe a length-val-offset could be stored,
			//then the length tallied up during writes, then a done() inline func called that writes the length at the right offset o_O
		}
		public void Draw(params Func<RegisterA>[] tile) {
			_liveQueue.Write(Y, () => {
				_liveQueue.Push(_opTileArray);
				_liveQueue.Push((U8)tile.Length);
				for (var i = 0; i < tile.Length; i++) { //using tile.length here so individual values can be pushed after this call
					_liveQueue.Push(tile[i].Invoke());
				}
			});
		}
		public void Draw_Manual(U8 len) {
			_liveQueue.PushStart(Y);
			_liveQueue.Push(_opTileArray);
			_liveQueue.Push(len);
		}
		public void Draw_Manual(VByte len) {
			_liveQueue.PushStart(Y);
			_liveQueue.Push(_opTileArray);
			_liveQueue.Push(len);
		}
		[CodeSection]
		private void Handler() {
			_liveQueue.Unsafe_Pop(Y);
			X.Set(_liveQueue.Unsafe_Peek(Y)); //Number of bytes of data
			Loop.Do(() => {
				_liveQueue.Unsafe_Pop(Y);
				NES.PPU.Data.Set(_liveQueue.Unsafe_Peek(Y));
				X--;
			}).While(() => X.NotEquals(0));
			GoTo(_executeLoopContinue);
		}
	}
}
