using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Editing
{
    internal class SimpleInputs
    {
        public static bool RightTrigger
        {
            get
            {
                return ControllerInputPoller.instance.rightControllerIndexFloat > 0.5f;
            }
        }
        public static bool RightGrab
        {
            get
            {
                return ControllerInputPoller.instance.rightGrab;
            }
        }
        public static bool RightA
        {
            get
            {
                return ControllerInputPoller.instance.rightControllerSecondaryButton;
            }
        }
        public static bool RightB
        {
            get
            {
                return ControllerInputPoller.instance.rightControllerSecondaryButton;
            }
        }
        public static bool LeftTrigger
        {
            get
            {
                return ControllerInputPoller.instance.leftControllerIndexFloat > 0.5f;
            }
        }
        public static bool LeftGrab
        {
            get
            {
                return ControllerInputPoller.instance.leftGrab;
            }
        }
        public static bool LeftX
        {
            get
            {
                return ControllerInputPoller.instance.leftControllerPrimaryButton;
            }
        }
        public static bool LeftY
        {
            get
            {
                return ControllerInputPoller.instance.leftControllerSecondaryButton;
            }
        }
    }
}
