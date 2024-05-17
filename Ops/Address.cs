using NESSharp.Common;
using NESSharp.Core;
using System;
using System.Collections.Generic;
using System.Text;
using static NESSharp.Core.CPU6502;

namespace NESSharp.Lib.VRamQueue.Ops {
	public class Address {
		private U8 _opAddr;
		private LiveQueue _liveQueue;
		private Label _executeLoopContinue;
		public Address(Func<Label, U8> handlerListAdd, LiveQueue queue, Label execContinue, Label _) {
			_liveQueue = queue;
			_executeLoopContinue = execContinue;
			_opAddr = handlerListAdd(AL.LabelFor(Handler));
		}
		public void SetU16(Core.Address addr) {
			_liveQueue.Write(Y, () => {
				_liveQueue.Push(_opAddr);
				_liveQueue.Push(addr.Hi);
				_liveQueue.Push(addr.Lo);
			});
		}
		public void SetWord(VWord n) {
			_liveQueue.Write(Y, () => {
				_liveQueue.Push(_opAddr);
				_liveQueue.Push(n.Hi);
				_liveQueue.Push(n.Lo);
			});
		}
		public void SetIOperands(Func<IOperand> Hi, Func<IOperand> Lo) {
			_liveQueue.Write(Y, () => {
				_liveQueue.Push(_opAddr);
				_liveQueue.Push(Hi());
				_liveQueue.Push(Lo());
			});
		}

		public void SetROM(U16 addr) {
			AL.Raw(_opAddr);
			AL.Raw(addr.Hi, addr.Lo);
		}

		[CodeSection]
		private void Handler() {
			_liveQueue.Unsafe_Pop(Y);
			NES.PPU.Reset();
			NES.PPU.Address.Set(_liveQueue.Unsafe_Peek(Y));
			_liveQueue.Unsafe_Pop(Y);
			NES.PPU.Address.Set(_liveQueue.Unsafe_Peek(Y));
			AL.GoTo(_executeLoopContinue);
		}
	}
}
