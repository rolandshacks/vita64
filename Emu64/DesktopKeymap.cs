using C64Lib;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace C64Emu
{
    class DesktopKeymap
    {
        public static int MODE_SHIFT = 0x0;

        public static int[] keymap = {

            // space
            (int) Keys.Space,       (int) KeyCode.C64KEY_SPACE,

            // row 1
            
            //(int) Keys.BACKQUOTE,         (int) KeyCode.C64KEY_LEFT_ARROW,
            
            (int) Keys.D1,                 (int) KeyCode.C64KEY_1,
            (int) Keys.D2,                 (int) KeyCode.C64KEY_2,
            (int) Keys.D3,                 (int) KeyCode.C64KEY_3,
            (int) Keys.D4,                 (int) KeyCode.C64KEY_4,
            (int) Keys.D5,                 (int) KeyCode.C64KEY_5,
            (int) Keys.D6,                 (int) KeyCode.C64KEY_6,
            (int) Keys.D7,                 (int) KeyCode.C64KEY_7,
            (int) Keys.D8,                 (int) KeyCode.C64KEY_8,
            (int) Keys.D9,                 (int) KeyCode.C64KEY_9,
            (int) Keys.D0,                 (int) KeyCode.C64KEY_0,

            /*
            (int) Keys.PLUS,              (int) KeyCode.C64KEY_PLUS,
            (int) Keys.MINUS,             (int) KeyCode.C64KEY_MINUS,
            (int) Keys.KP_MINUS,          (int) KeyCode.C64KEY_POUND,       // CHOOSE PROPER KEY
            (int) Keys.HOME,              (int) KeyCode.C64KEY_CLRHOME,     // CHOOSE PROPER KEY
            (int) Keys.BACKSPACE,         (int) KeyCode.C64KEY_INSTDEL,
            (int) Keys.INSERT,            (int) KeyCode.C64KEY_INSTDEL | (int) KeyCode.C64KEY_FLAG_SHIFT,
            */
            (int) Keys.Back,              (int) KeyCode.C64KEY_INSTDEL,

            // row 2
            // CTRL
            (int) Keys.Q,                 (int) KeyCode.C64KEY_Q,
            (int) Keys.W,                 (int) KeyCode.C64KEY_W,
            (int) Keys.E,                 (int) KeyCode.C64KEY_E,
            (int) Keys.R,                 (int) KeyCode.C64KEY_R,
            (int) Keys.T,                 (int) KeyCode.C64KEY_T,
            (int) Keys.Y,                 (int) KeyCode.C64KEY_Y,
            (int) Keys.U,                 (int) KeyCode.C64KEY_U,
            (int) Keys.I,                 (int) KeyCode.C64KEY_I,
            (int) Keys.O,                 (int) KeyCode.C64KEY_O,
            (int) Keys.P,                 (int) KeyCode.C64KEY_P,
            /*
            (int) Keys.KP_PLUS,           (int) KeyCode.C64KEY_AT,
            (int) Keys.BACKSLASH,         (int) KeyCode.C64KEY_ASTERISK,
            (int) Keys.ASTERISK,          (int) KeyCode.C64KEY_ASTERISK,
            (int) Keys.KP_MULTIPLY,       (int) KeyCode.C64KEY_ASTERISK,
            (int) Keys.KP_DIVIDE,         (int) KeyCode.C64KEY_UP_ARROW,
            (int) Keys.DELETE,            (int) KeyCode.C64KEY_RESTORE,
            */

            // row 3
            (int) Keys.Tab,               (int) KeyCode.C64KEY_RUNSTOP,
            //(int) Keys.Break,             (int) KeyCode.C64KEY_RUNSTOP,
            (int) Keys.CapsLock,          (int) KeyCode.C64KEY_LEFT_SHIFT,
            (int) Keys.A,                 (int) KeyCode.C64KEY_A,
            (int) Keys.S,                 (int) KeyCode.C64KEY_S,
            (int) Keys.D,                 (int) KeyCode.C64KEY_D,
            (int) Keys.F,                 (int) KeyCode.C64KEY_F,
            (int) Keys.G,                 (int) KeyCode.C64KEY_G,
            (int) Keys.H,                 (int) KeyCode.C64KEY_H,
            (int) Keys.J,                 (int) KeyCode.C64KEY_J,
            (int) Keys.K,                 (int) KeyCode.C64KEY_K,
            (int) Keys.L,                 (int) KeyCode.C64KEY_L,

            (int) Keys.OemSemicolon|MODE_SHIFT, (int) KeyCode.C64KEY_COLON,
            (int) Keys.OemQuotes,         (int) KeyCode.C64KEY_COLON,
            (int) Keys.OemSemicolon,      (int) KeyCode.C64KEY_SEMICOLON,
            (int) Keys.OemMinus,          (int) KeyCode.C64KEY_EQUAL,
            (int) Keys.Enter,             (int) KeyCode.C64KEY_RETURN,

            // row 4
            (int) Keys.LeftAlt,           (int) KeyCode.C64KEY_COMMODORE,
            (int) Keys.LeftShift,         (int) KeyCode.C64KEY_LEFT_SHIFT,
            (int) Keys.Z,                 (int) KeyCode.C64KEY_Z,
            (int) Keys.X,                 (int) KeyCode.C64KEY_X,
            (int) Keys.C,                 (int) KeyCode.C64KEY_C,
            (int) Keys.V,                 (int) KeyCode.C64KEY_V,
            (int) Keys.B,                 (int) KeyCode.C64KEY_B,
            (int) Keys.N,                 (int) KeyCode.C64KEY_N,
            (int) Keys.M,                 (int) KeyCode.C64KEY_M,
            (int) Keys.OemComma,             (int) KeyCode.C64KEY_COMMA,
            (int) Keys.OemPeriod,            (int) KeyCode.C64KEY_PERIOD,
            (int) Keys.OemQuestion,             (int) KeyCode.C64KEY_SLASH,
            (int) Keys.RightShift,            (int) KeyCode.C64KEY_RIGHT_SHIFT,
            (int) Keys.Left,              (int) KeyCode.C64KEY_CRSR_LEFTRIGHT | (int) KeyCode.C64KEY_FLAG_SHIFT,
            (int) Keys.Right,             (int) KeyCode.C64KEY_CRSR_LEFTRIGHT,
            (int) Keys.Up,                (int) KeyCode.C64KEY_CRSR_UPDOWN | (int) KeyCode.C64KEY_FLAG_SHIFT,
            (int) Keys.Down,              (int) KeyCode.C64KEY_CRSR_UPDOWN,

            // function keys
            (int) Keys.F1,                (int) KeyCode.C64KEY_F1F2,
            (int) Keys.F2,                (int) KeyCode.C64KEY_F1F2 | (int) KeyCode.C64KEY_FLAG_SHIFT,
            (int) Keys.F3,                (int) KeyCode.C64KEY_F3F4,
            (int) Keys.F4,                (int) KeyCode.C64KEY_F3F4 | (int) KeyCode.C64KEY_FLAG_SHIFT,
            (int) Keys.F5,                (int) KeyCode.C64KEY_F5F6,
            (int) Keys.F6,                (int) KeyCode.C64KEY_F5F6 | (int) KeyCode.C64KEY_FLAG_SHIFT,
            (int) Keys.F7,                (int) KeyCode.C64KEY_F7F8,
            (int) Keys.F8,                (int) KeyCode.C64KEY_F7F8 | (int) KeyCode.C64KEY_FLAG_SHIFT,
            
            (int) Keys.F9,                (int) KeyCode.COMMAND_DEBUGGER_TOGGLE,
            (int) Keys.F11,               (int) KeyCode.COMMAND_RESET,
            (int) Keys.F12,               (int) KeyCode.COMMAND_RESTORE,
            (int) 0,                      (int) KeyCode.COMMAND_SWAP_JOYSTICKS,

            0, 0,
        };

        public static int Translate(int SystemCode)
        {
            for (int i = 0; i < keymap.Length - 1; i += 2)
            {
                if (keymap[i] == SystemCode)
                {
                    return keymap[i + 1];
                }
            }

            return -1;
        }



    }
}
