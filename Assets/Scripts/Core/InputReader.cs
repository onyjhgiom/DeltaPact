using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace MoriEchoes
{
    public sealed class InputReader
    {
        public Vector2 Move { get; private set; }
        public Vector2 AimStick { get; private set; }
        public Vector2 Pointer { get; private set; }
        public Vector2 LookDelta { get; private set; }
        public bool AimDownSights { get; private set; }
        public bool Jump { get; private set; }
        public bool JumpHeld { get; private set; }
        public bool Crouch { get; private set; }
        public bool ChangeView { get; private set; }
        public bool Interact { get; private set; }
        public bool HasPointer { get; private set; }
        public bool UsingGamepad { get; private set; }
        public bool Fire { get; private set; }
        public bool Dodge { get; private set; }
        public bool LegacyDodge { get; private set; }
        public bool Reload { get; private set; }
        public bool Skill { get; private set; }
        public bool TechniqueOne { get; private set; }
        public bool TechniqueTwo { get; private set; }
        public bool Ultimate { get; private set; }
        public bool Pause { get; private set; }
        public int SwitchWeapon { get; private set; } = -1;

        public void Capture()
        {
            Move = Vector2.zero; AimStick = Vector2.zero; LookDelta = Vector2.zero;
            AimDownSights = false;
            Jump = JumpHeld = Crouch = ChangeView = Interact = false;
            Fire = Dodge = Reload = Skill = TechniqueOne = TechniqueTwo = Ultimate = Pause = false;
            LegacyDodge = false;
            SwitchWeapon = -1;
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;
            Gamepad gamepad = Gamepad.current;
            HasPointer = mouse != null;
            if (keyboard != null)
            {
                Move = new Vector2((keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0),
                    (keyboard.wKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0));
                Jump = keyboard.spaceKey.wasPressedThisFrame;
                LegacyDodge = Jump;
                JumpHeld = keyboard.spaceKey.isPressed;
                Dodge = keyboard.leftShiftKey.wasPressedThisFrame;
                Crouch = keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed;
                ChangeView = keyboard.tabKey.wasPressedThisFrame;
                Interact = keyboard.eKey.wasPressedThisFrame;
                Move = new Vector2(Move.x + (keyboard.rightArrowKey.isPressed ? 1 : 0) - (keyboard.leftArrowKey.isPressed ? 1 : 0),
                    Move.y + (keyboard.upArrowKey.isPressed ? 1 : 0) - (keyboard.downArrowKey.isPressed ? 1 : 0));
                Reload = keyboard.rKey.wasPressedThisFrame;
                Skill = keyboard.qKey.wasPressedThisFrame;
                TechniqueOne = keyboard.fKey.wasPressedThisFrame;
                TechniqueTwo = keyboard.cKey.wasPressedThisFrame;
                Ultimate = keyboard.xKey.wasPressedThisFrame;
                Pause = keyboard.escapeKey.wasPressedThisFrame;
                if (keyboard.digit1Key.wasPressedThisFrame) SwitchWeapon = 0;
                if (keyboard.digit2Key.wasPressedThisFrame) SwitchWeapon = 1;
                if (keyboard.digit3Key.wasPressedThisFrame) SwitchWeapon = 2;
                if (keyboard.digit4Key.wasPressedThisFrame) SwitchWeapon = 3;
                if (keyboard.digit5Key.wasPressedThisFrame) SwitchWeapon = 4;
                if (keyboard.digit6Key.wasPressedThisFrame) SwitchWeapon = 5;
                if (keyboard.digit7Key.wasPressedThisFrame) SwitchWeapon = 6;
                if (keyboard.digit8Key.wasPressedThisFrame) SwitchWeapon = 7;
                if (keyboard.digit9Key.wasPressedThisFrame) SwitchWeapon = 8;
                if (Move.sqrMagnitude > 0 || Dodge || Reload) UsingGamepad = false;
            }
            if (mouse != null)
            {
                Pointer = mouse.position.ReadValue();
                LookDelta = mouse.delta.ReadValue();
                AimDownSights = mouse.rightButton.isPressed;
                Fire = mouse.leftButton.isPressed;
                if (mouse.scroll.y.ReadValue() > 0) SwitchWeapon = -3;
                if (mouse.scroll.y.ReadValue() < 0) SwitchWeapon = -2;
                if (LookDelta.sqrMagnitude > 0.01f || Fire || AimDownSights) UsingGamepad = false;
            }
            if (gamepad != null)
            {
                Vector2 stick = gamepad.leftStick.ReadValue();
                AimStick = gamepad.rightStick.ReadValue();
                if (stick.sqrMagnitude > 0.04f) { Move = stick; UsingGamepad = true; }
                if (AimStick.sqrMagnitude > 0.04f) UsingGamepad = true;
                if (gamepad.rightTrigger.isPressed) { Fire = true; UsingGamepad = true; }
                if (gamepad.leftTrigger.isPressed) { AimDownSights = true; UsingGamepad = true; }
                Jump |= gamepad.buttonSouth.wasPressedThisFrame;
                JumpHeld |= gamepad.buttonSouth.isPressed;
                Dodge |= gamepad.buttonEast.wasPressedThisFrame;
                Crouch |= stick.y < -0.5f;
                ChangeView |= gamepad.selectButton.wasPressedThisFrame;
                Interact |= gamepad.dpad.up.wasPressedThisFrame;
                Reload |= gamepad.buttonWest.wasPressedThisFrame;
                Skill |= gamepad.buttonNorth.wasPressedThisFrame;
                TechniqueOne |= gamepad.dpad.left.wasPressedThisFrame;
                TechniqueTwo |= gamepad.dpad.right.wasPressedThisFrame;
                Ultimate |= gamepad.rightStickButton.wasPressedThisFrame;
                Pause |= gamepad.startButton.wasPressedThisFrame;
                if (gamepad.leftShoulder.wasPressedThisFrame) SwitchWeapon = -2;
                if (gamepad.rightShoulder.wasPressedThisFrame) SwitchWeapon = -3;
            }
            Move = Vector2.ClampMagnitude(Move, 1f);
#else
            HasPointer = false;
#endif
        }
    }
}
