using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace C64Lib
{
    public enum KeyCode
    {
        KEYCODE_MASK            = 0x00FF,
        KEYFLAG_MASK            = 0xFF00,

        KEYFLAG_NONE            = 0x0,
        KEYFLAG_PRESSED         = 0x100,
        KEYFLAG_RELEASED        = 0x200,
        KEYFLAG_COMMAND         = 0x400,

        C64KEY_FLAG_SHIFT       = 0x80,

        C64KEY_INSTDEL          = 0x00,
        C64KEY_RETURN           = 0x01,
        C64KEY_CRSR_LEFTRIGHT   = 0x02,
        C64KEY_F7F8             = 0x03,
        C64KEY_F1F2             = 0x04,
        C64KEY_F3F4             = 0x05,
        C64KEY_F5F6             = 0x06,
        C64KEY_CRSR_UPDOWN      = 0x07,
        C64KEY_3                = 0x08,
        C64KEY_W                = 0x09,
        C64KEY_A                = 0x0A,
        C64KEY_4                = 0x0B,
        C64KEY_Z                = 0x0C,
        C64KEY_S                = 0x0D,
        C64KEY_E                = 0x0E,
        C64KEY_LEFT_SHIFT       = 0x0F,   // unused

        C64KEY_5                = 0x10,
        C64KEY_R                = 0x11,
        C64KEY_D                = 0x12,
        C64KEY_6                = 0x13,
        C64KEY_C                = 0x14,
        C64KEY_F                = 0x15,
        C64KEY_T                = 0x16,
        C64KEY_X                = 0x17,
        C64KEY_7                = 0x18,
        C64KEY_Y                = 0x19,
        C64KEY_G                = 0x1A,
        C64KEY_8                = 0x1B,
        C64KEY_B                = 0x1C,
        C64KEY_H                = 0x1D,
        C64KEY_U                = 0x1E,
        C64KEY_V                = 0x1F,

        C64KEY_9                = 0x20,
        C64KEY_I                = 0x21,
        C64KEY_J                = 0x22,
        C64KEY_0                = 0x23,
        C64KEY_M                = 0x24,
        C64KEY_K                = 0x25,
        C64KEY_O                = 0x26,
        C64KEY_N                = 0x27,
        C64KEY_PLUS             = 0x28,
        C64KEY_P                = 0x29,
        C64KEY_L                = 0x2A,
        C64KEY_MINUS            = 0x2B,
        C64KEY_PERIOD           = 0x2C,    // .
        C64KEY_COLON            = 0x2D,    // [
        C64KEY_AT               = 0x2E,
        C64KEY_COMMA            = 0x2F,    // <

        C64KEY_QUESTIONMARK     = 0x30,
        C64KEY_POUND            = 0x30,

        C64KEY_ASTERISK         = 0x31,
        C64KEY_SEMICOLON        = 0x32,    // ]
        C64KEY_CLRHOME          = 0x33,
        C64KEY_RIGHT_SHIFT      = 0x34,    // unused
        C64KEY_EQUAL            = 0x35,
        C64KEY_UP_ARROW         = 0x36,
        C64KEY_SLASH            = 0x37,
        C64KEY_1                = 0x38,
        C64KEY_LEFT_ARROW       = 0x39,
        C64KEY_CONTROL          = 0x3A,   // unused
        C64KEY_2                = 0x3B,
        C64KEY_SPACE            = 0x3C,
        C64KEY_COMMODORE        = 0x3D,   // unused
        C64KEY_Q                = 0x3E,
        C64KEY_RUNSTOP          = 0x3F,

        // fake key: restore -> cause NMI (non maskable interrupt)
        C64KEY_RESTORE          = 0x40,

        C64STICK_NONE           = 0x0,
        C64STICK_FIRE           = 0x1,
        C64STICK_LEFT           = 0x2,
        C64STICK_RIGHT          = 0x4,
        C64STICK_UP             = 0x8,
        C64STICK_DOWN           = 0x10,

        COMMAND_UNKNOWN         = 0 | KEYFLAG_COMMAND,
        COMMAND_TRUEDRIVE_ON    = 1 | KEYFLAG_COMMAND,
        COMMAND_TRUEDRIVE_OFF   = 2 | KEYFLAG_COMMAND,
        COMMAND_RESET           = 3 | KEYFLAG_COMMAND,
        COMMAND_DEBUGGER_TOGGLE = 4 | KEYFLAG_COMMAND,
        COMMAND_RESTORE         = 5 | KEYFLAG_COMMAND,
        COMMAND_SWAP_JOYSTICKS  = 6 | KEYFLAG_COMMAND,
    
    }
}
