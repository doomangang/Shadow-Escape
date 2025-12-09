#if ENABLE_INPUT_SYSTEM
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace ShadowEscape
{
    /// <summary>
    /// Creates a lightweight Input System action asset for UI navigation so runtime-built canvases
    /// can receive pointer and gamepad input without hand-authored assets.
    /// </summary>
    public static class RuntimeUIInputActions
    {
        private static InputActionAsset _uiActions;

        public static bool ConfigureModule(InputSystemUIInputModule module)
        {
            if (module == null)
            {
                return false;
            }

            var asset = EnsureAsset();
            if (asset == null)
            {
                return false;
            }

            module.actionsAsset = asset;
            module.point = InputActionReference.Create(asset.FindAction("UI/Point", true));
            module.move = InputActionReference.Create(asset.FindAction("UI/Move", true));
            module.submit = InputActionReference.Create(asset.FindAction("UI/Submit", true));
            module.cancel = InputActionReference.Create(asset.FindAction("UI/Cancel", true));
            module.leftClick = InputActionReference.Create(asset.FindAction("UI/LeftClick", true));
            module.rightClick = InputActionReference.Create(asset.FindAction("UI/RightClick", true));
            module.middleClick = InputActionReference.Create(asset.FindAction("UI/MiddleClick", true));
            module.scrollWheel = InputActionReference.Create(asset.FindAction("UI/ScrollWheel", true));
            module.trackedDeviceOrientation = InputActionReference.Create(asset.FindAction("UI/TrackedDeviceOrientation", true));
            module.trackedDevicePosition = InputActionReference.Create(asset.FindAction("UI/TrackedDevicePosition", true));
            return true;
        }

        private static InputActionAsset EnsureAsset()
        {
            if (_uiActions != null)
            {
                return _uiActions;
            }

            var asset = ScriptableObject.CreateInstance<InputActionAsset>();
            asset.name = "RuntimeUIActions";
            asset.hideFlags = HideFlags.HideAndDontSave;

            var uiMap = new InputActionMap("UI");

            var point = uiMap.AddAction("Point", InputActionType.PassThrough);
            point.expectedControlType = "Vector2";
            point.AddBinding("<Pointer>/position");

            var move = uiMap.AddAction("Move", InputActionType.PassThrough);
            move.expectedControlType = "Vector2";
            var moveComposite = move.AddCompositeBinding("2DVector(mode=2)");
            moveComposite.With("Up", "<Keyboard>/w");
            moveComposite.With("Up", "<Keyboard>/upArrow");
            moveComposite.With("Down", "<Keyboard>/s");
            moveComposite.With("Down", "<Keyboard>/downArrow");
            moveComposite.With("Left", "<Keyboard>/a");
            moveComposite.With("Left", "<Keyboard>/leftArrow");
            moveComposite.With("Right", "<Keyboard>/d");
            moveComposite.With("Right", "<Keyboard>/rightArrow");
            move.AddBinding("<Gamepad>/leftStick");
            move.AddBinding("<Gamepad>/dpad");
            move.AddBinding("<Joystick>/stick");

            var submit = uiMap.AddAction("Submit", InputActionType.PassThrough);
            submit.expectedControlType = "Button";
            submit.AddBinding("<Keyboard>/enter");
            submit.AddBinding("<Keyboard>/numpadEnter");
            submit.AddBinding("<Keyboard>/space");
            submit.AddBinding("<Gamepad>/buttonSouth");

            var cancel = uiMap.AddAction("Cancel", InputActionType.PassThrough);
            cancel.expectedControlType = "Button";
            cancel.AddBinding("<Keyboard>/escape");
            cancel.AddBinding("<Gamepad>/buttonEast");

            var leftClick = uiMap.AddAction("LeftClick", InputActionType.PassThrough);
            leftClick.expectedControlType = "Button";
            leftClick.AddBinding("<Mouse>/leftButton");
            leftClick.AddBinding("<Pen>/tip");
            leftClick.AddBinding("<Touchscreen>/touch*/press");

            var rightClick = uiMap.AddAction("RightClick", InputActionType.PassThrough);
            rightClick.expectedControlType = "Button";
            rightClick.AddBinding("<Mouse>/rightButton");

            var middleClick = uiMap.AddAction("MiddleClick", InputActionType.PassThrough);
            middleClick.expectedControlType = "Button";
            middleClick.AddBinding("<Mouse>/middleButton");

            var scroll = uiMap.AddAction("ScrollWheel", InputActionType.PassThrough);
            scroll.expectedControlType = "Vector2";
            scroll.AddBinding("<Mouse>/scroll");

            var trackedPos = uiMap.AddAction("TrackedDevicePosition", InputActionType.PassThrough);
            trackedPos.expectedControlType = "Vector3";
            trackedPos.AddBinding("<XRController>/devicePosition");
            trackedPos.AddBinding("<TrackedDevice>/devicePosition");

            var trackedRot = uiMap.AddAction("TrackedDeviceOrientation", InputActionType.PassThrough);
            trackedRot.expectedControlType = "Quaternion";
            trackedRot.AddBinding("<XRController>/deviceRotation");
            trackedRot.AddBinding("<TrackedDevice>/deviceRotation");

            asset.AddActionMap(uiMap);
            uiMap.Enable();

            _uiActions = asset;
            return _uiActions;
        }
    }
}
#endif
