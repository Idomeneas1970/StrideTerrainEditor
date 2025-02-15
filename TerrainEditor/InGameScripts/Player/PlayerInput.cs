﻿// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Events;
using Stride.Input;
using HeightMapEditor;
using TerrainEditor;

namespace StrideTerrain.Player
{
    public class PlayerInput : SyncScript
    {
        /// <summary>
        /// Raised every frame with the intended direction of movement from the player.
        /// </summary>
        // TODO Should not be static, but allow binding between player and controller
        public static readonly EventKey<Vector3> MoveDirectionEventKey = new EventKey<Vector3>();

        public static readonly EventKey<Vector2> CameraDirectionEventKey = new EventKey<Vector2>();

        public static readonly EventKey<bool> JumpEventKey = new EventKey<bool>();
  
        //      private readonly EventReceiver<CameraType> CameraTypeEventKey = new EventReceiver<CameraType>(MultiTypeCameraController.CameraTypeEventKey);
        
        private bool jumpButtonDown = false;

        public float DeadZone { get; set; } = 0.25f;

        public CameraComponent Camera { get; set; }

        /// <summary>
        /// Multiplies move movement by this amount to apply aim rotations
        /// </summary>
        public float MouseSensitivity = 1f;

        public List<Keys> KeysLeft { get; } = new List<Keys>();

        public List<Keys> KeysRight { get; } = new List<Keys>();

        public List<Keys> KeysUp { get; } = new List<Keys>();

        public List<Keys> KeysDown { get; } = new List<Keys>();

        public List<Keys> KeysJump { get; } = new List<Keys>();

        public bool ContinuousMovement { get; set; }
        public override void Update()
        {
           // return;
            if (TerrainEditorView.CurrentEditorMode !=
                EditorMode.InGameTerrain) return;
            if (MultiTypeCameraController.CameraType == CameraType.FreeMovement) return;
            // Character movement: should be camera-aware
            {
                // Left stick: movement
                var moveDirection = Input.GetLeftThumbAny(DeadZone);
                float speed=Input.IsKeyDown(Keys.LeftShift) ? 10.0f : 1.0f;
                // Keyboard: movement
                if (KeysLeft.Any(key => Input.IsKeyDown(key)))
                    moveDirection += -Vector2.UnitX;
                if (KeysRight.Any(key => Input.IsKeyDown(key)))
                    moveDirection += +Vector2.UnitX;
                if (KeysUp.Any(key => Input.IsKeyDown(key)))
                    moveDirection += +Vector2.UnitY;
                if (KeysDown.Any(key => Input.IsKeyDown(key)))
                    moveDirection += -Vector2.UnitY;
                if (Input.IsKeyPressed(Keys.X))
                {
                    ContinuousMovement = !ContinuousMovement;
                }
                if(ContinuousMovement) moveDirection += Vector2.UnitY;
                // Broadcast the movement vector as a world-space Vector3 to allow characters to be controlled
                var worldSpeed = (Camera != null) ? 
                    Utils.LogicDirectionToWorldDirection(moveDirection, Camera,
                    Vector3.UnitY): new Vector3(moveDirection.X, 0, 
                    moveDirection.Y);

                // Adjust vector's magnitute - worldSpeed has been normalized
                var moveLength = moveDirection.Length();
                var isDeadZoneLeft = moveLength < DeadZone;
                if (isDeadZoneLeft)
                {
                    worldSpeed = Vector3.Zero;
                }
                else
                {
                    if (moveLength > 1)
                    {
                        moveLength = 1;
                    }
                    else
                    {
                        moveLength = (moveLength - DeadZone) / (1f - DeadZone);
                    }

                    worldSpeed *= moveLength;
                }
                worldSpeed *= speed;

                MoveDirectionEventKey.Broadcast(worldSpeed);
            }

            // Camera rotation: left-right rotates the camera horizontally while up-down controls its altitude
            {
                // Right stick: camera rotation
                var cameraDirection = Input.GetRightThumbAny(DeadZone);
                var isDeadZoneRight = cameraDirection.Length() < DeadZone;
                if (isDeadZoneRight)
                    cameraDirection = Vector2.Zero;
                else
                    cameraDirection.Normalize();

                // Contrary to a mouse, driving camera rotation from a stick must be scaled by delta time.
                // The amount of camera rotation with a stick is constant over time based on the tilt of the stick,
                // Whereas mouse driven rotation is already constrained by time, it is driven by the difference in position from last *time* to this *time*.
                cameraDirection *= (float)this.Game.UpdateTime.Elapsed.TotalSeconds;

//                CameraTypeEventKey.TryReceive(out CameraType1);
                if (MultiTypeCameraController.CameraType == CameraType.ThirdPerson ||
                    MultiTypeCameraController.CameraType == CameraType.FirstPerson ||
                    MultiTypeCameraController.CameraType == CameraType.Shoulder)
                {
                    cameraDirection += new Vector2(Input.MouseDelta.X, -Input.MouseDelta.Y) * MouseSensitivity;
                }
                else if (MultiTypeCameraController.CameraType == CameraType.Isometric)
                {
                    //no mouse movement
                   // cameraDirection += new Vector2(Input.MouseDelta.X, 0) * MouseSensitivity;
                }
                // Broadcast the camera direction directly, as a screen-space Vector2
                CameraDirectionEventKey.Broadcast(cameraDirection);
            }

            // Jumping: don't bother with jump restrictions here, just pass the button states
            {
                // Controller: jumping
                var isJumpDown = Input.IsGamePadButtonDownAny(GamePadButton.A);
                var didJump = (!jumpButtonDown && isJumpDown);
                jumpButtonDown = isJumpDown;

                // Keyboard: jumping
                didJump |= (KeysJump.Any(key => Input.IsKeyPressed(key)));

                JumpEventKey.Broadcast(didJump);
            }
        }
    }
}
