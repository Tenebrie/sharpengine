using Engine.Core.Common;
using Engine.Testing.TestUtilities.Mocks;
using Engine.Core.EntitySystem.Entities.BuiltIns;
using Engine.Core.EntitySystem.Services;
using Engine.Core.Input.Attributes;
using Engine.Core.Input.Contexts;
using Silk.NET.Input;
using InputService = Engine.Core.EntitySystem.Services.InputService;

namespace Engine.Testing.Worlds.Services;

public abstract class InputServiceTest
{
    public class WithHardcodedKeys
    {
        private static InputBackstage SetupBackstage()
        {
            var backstage = new InputBackstage
            {
                Keyboard = new MockKeyboard()
            };
            backstage.GetService<InputService>().BindKeyboardEvents(backstage.Keyboard);
            backstage.Initialize();
            return backstage;
        }
        
        internal class InputBackstage() : StandaloneBackstage(skipInit: true)
        {
            public required MockKeyboard Keyboard;
            
            // OnInput event handlers
            public int ActionCallCount;
            public double ActionWithDoubleCallSum;
            public Vector2 ActionWithVectorCallSum;

            [OnKeyInput(Key.Number0)]
            private void OnAction() => ActionCallCount += 1;
            [OnKeyInput(Key.Number1, 1.0)]
            private void OnActionWithDouble(double value) => ActionWithDoubleCallSum += value;
            [OnKeyInput(Key.Number2, 1.0, 2.0)]
            private void OnActionWithVector(Vector2 data) => ActionWithVectorCallSum += data;
            
            // OnInputHeld event handlers
            public int ActionHeldCallCount;
            public double ActionHeldCallSum;
            public double ActionHeldWithDoubleCallSum;
            public Vector2 ActionHeldWithVectorCallSum;

            [OnKeyInputHeld(Key.Number0)]
            private void OnSimpleActionHeld() => ActionHeldCallCount += 1;
            [OnKeyInputHeld(Key.Number0)]
            private void OnSimpleActionHeld(double deltaTime) => ActionHeldCallSum += deltaTime;
            [OnKeyInputHeld(Key.Number1, 1.0)]
            private void OnWithDoubleActionHeld(double deltaTime, double value) => ActionHeldWithDoubleCallSum += value;
            [OnKeyInputHeld(Key.Number2, 1.0, 2.0)]
            private void OnWithVector2ActionHeld(double deltaTime, Vector2 data) => ActionHeldWithVectorCallSum += data;
            
            // OnInputReleased event handlers
            public int ActionReleasedCallCount;
            public double ActionReleasedWithDoubleCallSum;
            public Vector2 ActionReleasedWithVectorCallSum;

            [OnKeyInputReleased(Key.Number0)]
            private void OnSimpleActionReleased() => ActionReleasedCallCount += 1;
            [OnKeyInputReleased(Key.Number1, 1.0)]
            private void OnWithDoubleActionReleased(double value) => ActionReleasedWithDoubleCallSum += value;
            [OnKeyInputReleased(Key.Number2, 1.0, 2.0)]
            private void OnWithVector2ActionReleased(Vector2 data) => ActionReleasedWithVectorCallSum += data;
            
            // Multiple key bound actions
            public int MultiActionCallCount;
            
            [OnKeyInput(Key.A)]
            [OnKeyInput(Key.B)]
            [OnKeyInput(Key.C)]
            [OnKeyInput(Key.D)]
            private void OnMultiAction() => MultiActionCallCount += 1;
        }
        
        [Fact]
        private void NotifiesSubscribersOnInputEvent()
        {
            var backstage = SetupBackstage();
            
            backstage.Keyboard.SendKeyDown(Key.Number0);
            Assert.Equal(1, backstage.ActionCallCount);
            Assert.Equal(0, backstage.ActionWithDoubleCallSum);
            Assert.Equal(Vector2.Zero, backstage.ActionWithVectorCallSum);
            Assert.Equal(0, backstage.ActionHeldCallCount);
            Assert.Equal(0, backstage.ActionHeldWithDoubleCallSum);
            Assert.Equal(Vector2.Zero, backstage.ActionHeldWithVectorCallSum);
            Assert.Equal(0, backstage.ActionReleasedCallCount);
            Assert.Equal(0, backstage.ActionReleasedWithDoubleCallSum);
            Assert.Equal(Vector2.Zero, backstage.ActionReleasedWithVectorCallSum);
            
            backstage.Keyboard.SendKeyUp(Key.Number0);
            Assert.Equal(1, backstage.ActionCallCount);
            Assert.Equal(0, backstage.ActionWithDoubleCallSum);
            Assert.Equal(Vector2.Zero, backstage.ActionWithVectorCallSum);
            Assert.Equal(0, backstage.ActionHeldCallCount);
            Assert.Equal(0, backstage.ActionHeldWithDoubleCallSum);
            Assert.Equal(Vector2.Zero, backstage.ActionHeldWithVectorCallSum);
            Assert.Equal(1, backstage.ActionReleasedCallCount);
            Assert.Equal(0, backstage.ActionReleasedWithDoubleCallSum);
            Assert.Equal(Vector2.Zero, backstage.ActionReleasedWithVectorCallSum);
        }
        
        [Fact]
        private void NotifiesSubscribersOnInputEventWithDouble()
        {
            var backstage = SetupBackstage();
            
            backstage.Keyboard.SendKeyDown(Key.Number1);
            Assert.Equal(0, backstage.ActionCallCount);
            Assert.Equal(1, backstage.ActionWithDoubleCallSum);
            Assert.Equal(Vector2.Zero, backstage.ActionWithVectorCallSum);
            Assert.Equal(0, backstage.ActionHeldCallCount);
            Assert.Equal(0, backstage.ActionHeldWithDoubleCallSum);
            Assert.Equal(Vector2.Zero, backstage.ActionHeldWithVectorCallSum);
            Assert.Equal(0, backstage.ActionReleasedCallCount);
            Assert.Equal(0, backstage.ActionReleasedWithDoubleCallSum);
            Assert.Equal(Vector2.Zero, backstage.ActionReleasedWithVectorCallSum);
            
            backstage.Keyboard.SendKeyUp(Key.Number1);
            Assert.Equal(0, backstage.ActionCallCount);
            Assert.Equal(1, backstage.ActionWithDoubleCallSum);
            Assert.Equal(Vector2.Zero, backstage.ActionWithVectorCallSum);
            Assert.Equal(0, backstage.ActionHeldCallCount);
            Assert.Equal(0, backstage.ActionHeldWithDoubleCallSum);
            Assert.Equal(Vector2.Zero, backstage.ActionHeldWithVectorCallSum);
            Assert.Equal(0, backstage.ActionReleasedCallCount);
            Assert.Equal(1, backstage.ActionReleasedWithDoubleCallSum);
            Assert.Equal(Vector2.Zero, backstage.ActionReleasedWithVectorCallSum);
        }
        
        [Fact]
        private void NotifiesSubscribersOnInputEventWithVector()
        {
            var backstage = SetupBackstage();
            
            backstage.Keyboard.SendKeyDown(Key.Number2);
            Assert.Equal(0, backstage.ActionCallCount);
            Assert.Equal(0, backstage.ActionWithDoubleCallSum);
            Assert.Equal(new Vector2(1.0, 2.0), backstage.ActionWithVectorCallSum);
            Assert.Equal(0, backstage.ActionHeldCallCount);
            Assert.Equal(0, backstage.ActionHeldWithDoubleCallSum);
            Assert.Equal(Vector2.Zero, backstage.ActionHeldWithVectorCallSum);
            Assert.Equal(0, backstage.ActionReleasedCallCount);
            Assert.Equal(0, backstage.ActionReleasedWithDoubleCallSum);
            Assert.Equal(Vector2.Zero, backstage.ActionReleasedWithVectorCallSum);
            
            backstage.Keyboard.SendKeyUp(Key.Number2);
            Assert.Equal(0, backstage.ActionCallCount);
            Assert.Equal(0, backstage.ActionWithDoubleCallSum);
            Assert.Equal(new Vector2(1.0, 2.0), backstage.ActionWithVectorCallSum);
            Assert.Equal(0, backstage.ActionHeldCallCount);
            Assert.Equal(0, backstage.ActionHeldWithDoubleCallSum);
            Assert.Equal(Vector2.Zero, backstage.ActionHeldWithVectorCallSum);
            Assert.Equal(0, backstage.ActionReleasedCallCount);
            Assert.Equal(0, backstage.ActionReleasedWithDoubleCallSum);
            Assert.Equal(new Vector2(1.0, 2.0), backstage.ActionReleasedWithVectorCallSum);
        }
        
        [Fact]
        private void AvoidsHeldSubscribersBeforeFrameUpdate()
        {
            var backstage = SetupBackstage();
            backstage.Keyboard.SendKeyDown(Key.Number0);
            backstage.Keyboard.SendKeyDown(Key.Number1);
            backstage.Keyboard.SendKeyDown(Key.Number2);
            Assert.Equal(0, backstage.ActionHeldCallCount);
            Assert.Equal(0, backstage.ActionHeldCallSum);
            Assert.Equal(0, backstage.ActionHeldWithDoubleCallSum);
            Assert.Equal(Vector2.Zero, backstage.ActionHeldWithVectorCallSum);
        }
        
        [Fact]
        private void AvoidsHeldSubscribersIfKeyIsNotPressed()
        {
            var backstage = SetupBackstage();
            backstage.ProcessLogicFrame(4.2);
            Assert.Equal(0, backstage.ActionHeldCallCount);
            Assert.Equal(0, backstage.ActionHeldCallSum);
            Assert.Equal(0, backstage.ActionHeldWithDoubleCallSum);
            Assert.Equal(Vector2.Zero, backstage.ActionHeldWithVectorCallSum);
        }
        
        [Fact]
        private void AvoidsHeldSubscribersIfKeyIsReleased()
        {
            var backstage = SetupBackstage();
            backstage.Keyboard.SendKeyDown(Key.Number0);
            backstage.Keyboard.SendKeyDown(Key.Number1);
            backstage.Keyboard.SendKeyDown(Key.Number2);
            backstage.Keyboard.SendKeyUp(Key.Number0);
            backstage.Keyboard.SendKeyUp(Key.Number1);
            backstage.Keyboard.SendKeyUp(Key.Number2);
            backstage.ProcessLogicFrame(4.2);
            Assert.Equal(0, backstage.ActionHeldCallCount);
            Assert.Equal(0, backstage.ActionHeldCallSum);
            Assert.Equal(0, backstage.ActionHeldWithDoubleCallSum);
            Assert.Equal(Vector2.Zero, backstage.ActionHeldWithVectorCallSum);
        }
        
        [Fact]
        private void NotifiesSubscribersOnInputHeldEvent()
        {
            var backstage = SetupBackstage();
            backstage.Keyboard.SendKeyDown(Key.Number0);
            backstage.ProcessLogicFrame(4.2);
            Assert.Equal(1, backstage.ActionHeldCallCount);
            Assert.Equal(4.2, backstage.ActionHeldCallSum);
            Assert.Equal(0, backstage.ActionHeldWithDoubleCallSum);
            Assert.Equal(Vector2.Zero, backstage.ActionHeldWithVectorCallSum);
        }
        
        [Fact]
        private void NotifiesSubscribersOnInputHeldEventWithDouble()
        {
            var backstage = SetupBackstage();
            backstage.Keyboard.SendKeyDown(Key.Number1);
            backstage.ProcessLogicFrame(4.2);
            Assert.Equal(0, backstage.ActionHeldCallCount);
            Assert.Equal(1, backstage.ActionHeldWithDoubleCallSum);
            Assert.Equal(Vector2.Zero, backstage.ActionHeldWithVectorCallSum);
        }
        
        [Fact]
        private void NotifiesSubscribersOnInputHeldEventWithVector()
        {
            var backstage = SetupBackstage();
            backstage.Keyboard.SendKeyDown(Key.Number2);
            backstage.ProcessLogicFrame(4.2);
            Assert.Equal(0, backstage.ActionHeldCallCount);
            Assert.Equal(0, backstage.ActionHeldWithDoubleCallSum);
            Assert.Equal(new Vector2(1, 2), backstage.ActionHeldWithVectorCallSum);
        }
        
        [Fact]
        private void NotifiesSubscribersEveryTimeForMultiActions()
        {
            var backstage = SetupBackstage();
            backstage.Keyboard.SendKeyDown(Key.A);
            backstage.Keyboard.SendKeyDown(Key.B);
            backstage.Keyboard.SendKeyDown(Key.C);
            backstage.Keyboard.SendKeyDown(Key.D);
            Assert.Equal(4, backstage.MultiActionCallCount);
        }
    }
    public class WithActions
    {
        private enum UserInputActions
        {
            Simple,
            WithDouble,
            WithVector,
            Up,
            Down,
            Left,
            Right,
        }

        private static InputBackstage SetupBackstage()
        {
            var backstage = new InputBackstage
            {
                Keyboard = new MockKeyboard()
            };
            backstage.GetService<ReflectionService>().SetUserInputActionsEnum<UserInputActions>();
            backstage.GetService<InputService>().BindKeyboardEvents(backstage.Keyboard);
            backstage.GetService<InputService>().InputContext = InputContext.GetBuilder<UserInputActions>()
                .Add(UserInputActions.Simple, Key.Number0)
                .Add(UserInputActions.WithDouble, Key.Number1)
                .Add(UserInputActions.WithVector, Key.Number2)
                .Add(UserInputActions.Up, Key.Up)
                .Add(UserInputActions.Down, Key.Down)
                .Add(UserInputActions.Left, Key.Left)
                .Add(UserInputActions.Right, Key.Right)
                .Add(UserInputActions.Up, Key.W)
                .Add(UserInputActions.Down, Key.S)
                .Add(UserInputActions.Left, Key.A)
                .Add(UserInputActions.Right, Key.D)
                .Build();
            backstage.Initialize();
            return backstage;
        }
        
        internal class InputBackstage() : StandaloneBackstage(skipInit: true)
        {
            public required MockKeyboard Keyboard;
            
            // OnInput event handlers
            public int ActionCallCount;
            public double ActionWithDoubleCallSum;
            public Vector2 ActionWithVectorCallSum;

            [OnInput<UserInputActions>(UserInputActions.Simple)]
            private void OnAction() => ActionCallCount += 1;
            [OnInput<UserInputActions>(UserInputActions.WithDouble, 1.0)]
            private void OnActionWithDouble(double value) => ActionWithDoubleCallSum += value;
            [OnInput<UserInputActions>(UserInputActions.WithVector, 1.0, 2.0)]
            private void OnActionWithVector(Vector2 data) => ActionWithVectorCallSum += data;
            
            // OnInputHeld event handlers
            public int ActionHeldCallCount;
            public double ActionHeldCallSum;
            public double ActionHeldWithDoubleCallSum;
            public Vector2 ActionHeldWithVectorCallSum;

            [OnInputHeld<UserInputActions>(UserInputActions.Simple)]
            private void OnSimpleActionHeld() => ActionHeldCallCount += 1;
            [OnInputHeld<UserInputActions>(UserInputActions.Simple)]
            private void OnSimpleActionHeld(double deltaTime) => ActionHeldCallSum += deltaTime;
            [OnInputHeld<UserInputActions>(UserInputActions.WithDouble, 1.0)]
            private void OnWithDoubleActionHeld(double deltaTime, double value) => ActionHeldWithDoubleCallSum += value;
            [OnInputHeld<UserInputActions>(UserInputActions.WithVector, 1.0, 2.0)]
            private void OnWithVector2ActionHeld(double deltaTime, Vector2 data) => ActionHeldWithVectorCallSum += data;
            
            // OnInputReleased event handlers
            public int ActionReleasedCallCount;
            public double ActionReleasedWithDoubleCallSum;
            public Vector2 ActionReleasedWithVectorCallSum;

            [OnInputReleased<UserInputActions>(UserInputActions.Simple)]
            private void OnSimpleActionReleased() => ActionReleasedCallCount += 1;
            [OnInputReleased<UserInputActions>(UserInputActions.WithDouble, 1.0)]
            private void OnWithDoubleActionReleased(double value) => ActionReleasedWithDoubleCallSum += value;
            [OnInputReleased<UserInputActions>(UserInputActions.WithVector, 1.0, 2.0)]
            private void OnWithVector2ActionReleased(Vector2 data) => ActionReleasedWithVectorCallSum += data;
            
            // Multiple key bound actions
            public int MultiActionWithVectorCallCount;
            public Vector2 MultiActionWithVectorCallSum;
            
            [OnInputHeld<UserInputActions>(UserInputActions.Up, 0.0, 1.0)]
            [OnInputHeld<UserInputActions>(UserInputActions.Down, 0.0, -1.0)]
            [OnInputHeld<UserInputActions>(UserInputActions.Left, -1.0, 0.0)]
            [OnInputHeld<UserInputActions>(UserInputActions.Right, 1.0, 0.0)]
            private void OnMultiActionWithVector(double deltaTime, Vector2 direction)
            {
                MultiActionWithVectorCallCount += 1;
                MultiActionWithVectorCallSum += direction;
            }
        }
        
        [Fact]
        private void NotifiesSubscribersOnInputEvent()
        {
            var backstage = SetupBackstage();
            
            backstage.Keyboard.SendKeyDown(Key.Number0);
            Assert.Equal(1, backstage.ActionCallCount);
            Assert.Equal(0, backstage.ActionWithDoubleCallSum);
            Assert.Equal(Vector2.Zero, backstage.ActionWithVectorCallSum);
            Assert.Equal(0, backstage.ActionHeldCallCount);
            Assert.Equal(0, backstage.ActionHeldWithDoubleCallSum);
            Assert.Equal(Vector2.Zero, backstage.ActionHeldWithVectorCallSum);
            Assert.Equal(0, backstage.ActionReleasedCallCount);
            Assert.Equal(0, backstage.ActionReleasedWithDoubleCallSum);
            Assert.Equal(Vector2.Zero, backstage.ActionReleasedWithVectorCallSum);
            
            backstage.Keyboard.SendKeyUp(Key.Number0);
            Assert.Equal(1, backstage.ActionCallCount);
            Assert.Equal(0, backstage.ActionWithDoubleCallSum);
            Assert.Equal(Vector2.Zero, backstage.ActionWithVectorCallSum);
            Assert.Equal(0, backstage.ActionHeldCallCount);
            Assert.Equal(0, backstage.ActionHeldWithDoubleCallSum);
            Assert.Equal(Vector2.Zero, backstage.ActionHeldWithVectorCallSum);
            Assert.Equal(1, backstage.ActionReleasedCallCount);
            Assert.Equal(0, backstage.ActionReleasedWithDoubleCallSum);
            Assert.Equal(Vector2.Zero, backstage.ActionReleasedWithVectorCallSum);
        }
        
        [Fact]
        private void NotifiesSubscribersOnInputEventWithDouble()
        {
            var backstage = SetupBackstage();
            
            backstage.Keyboard.SendKeyDown(Key.Number1);
            Assert.Equal(0, backstage.ActionCallCount);
            Assert.Equal(1, backstage.ActionWithDoubleCallSum);
            Assert.Equal(Vector2.Zero, backstage.ActionWithVectorCallSum);
            Assert.Equal(0, backstage.ActionHeldCallCount);
            Assert.Equal(0, backstage.ActionHeldWithDoubleCallSum);
            Assert.Equal(Vector2.Zero, backstage.ActionHeldWithVectorCallSum);
            Assert.Equal(0, backstage.ActionReleasedCallCount);
            Assert.Equal(0, backstage.ActionReleasedWithDoubleCallSum);
            Assert.Equal(Vector2.Zero, backstage.ActionReleasedWithVectorCallSum);
            
            backstage.Keyboard.SendKeyUp(Key.Number1);
            Assert.Equal(0, backstage.ActionCallCount);
            Assert.Equal(1, backstage.ActionWithDoubleCallSum);
            Assert.Equal(Vector2.Zero, backstage.ActionWithVectorCallSum);
            Assert.Equal(0, backstage.ActionHeldCallCount);
            Assert.Equal(0, backstage.ActionHeldWithDoubleCallSum);
            Assert.Equal(Vector2.Zero, backstage.ActionHeldWithVectorCallSum);
            Assert.Equal(0, backstage.ActionReleasedCallCount);
            Assert.Equal(1, backstage.ActionReleasedWithDoubleCallSum);
            Assert.Equal(Vector2.Zero, backstage.ActionReleasedWithVectorCallSum);
        }
        
        [Fact]
        private void NotifiesSubscribersOnInputEventWithVector()
        {
            var backstage = SetupBackstage();
            
            backstage.Keyboard.SendKeyDown(Key.Number2);
            Assert.Equal(0, backstage.ActionCallCount);
            Assert.Equal(0, backstage.ActionWithDoubleCallSum);
            Assert.Equal(new Vector2(1.0, 2.0), backstage.ActionWithVectorCallSum);
            Assert.Equal(0, backstage.ActionHeldCallCount);
            Assert.Equal(0, backstage.ActionHeldWithDoubleCallSum);
            Assert.Equal(Vector2.Zero, backstage.ActionHeldWithVectorCallSum);
            Assert.Equal(0, backstage.ActionReleasedCallCount);
            Assert.Equal(0, backstage.ActionReleasedWithDoubleCallSum);
            Assert.Equal(Vector2.Zero, backstage.ActionReleasedWithVectorCallSum);
            
            backstage.Keyboard.SendKeyUp(Key.Number2);
            Assert.Equal(0, backstage.ActionCallCount);
            Assert.Equal(0, backstage.ActionWithDoubleCallSum);
            Assert.Equal(new Vector2(1.0, 2.0), backstage.ActionWithVectorCallSum);
            Assert.Equal(0, backstage.ActionHeldCallCount);
            Assert.Equal(0, backstage.ActionHeldWithDoubleCallSum);
            Assert.Equal(Vector2.Zero, backstage.ActionHeldWithVectorCallSum);
            Assert.Equal(0, backstage.ActionReleasedCallCount);
            Assert.Equal(0, backstage.ActionReleasedWithDoubleCallSum);
            Assert.Equal(new Vector2(1.0, 2.0), backstage.ActionReleasedWithVectorCallSum);
        }
        
        [Fact]
        private void AvoidsHeldSubscribersBeforeFrameUpdate()
        {
            var backstage = SetupBackstage();
            backstage.Keyboard.SendKeyDown(Key.Number0);
            backstage.Keyboard.SendKeyDown(Key.Number1);
            backstage.Keyboard.SendKeyDown(Key.Number2);
            Assert.Equal(0, backstage.ActionHeldCallCount);
            Assert.Equal(0, backstage.ActionHeldCallSum);
            Assert.Equal(0, backstage.ActionHeldWithDoubleCallSum);
            Assert.Equal(Vector2.Zero, backstage.ActionHeldWithVectorCallSum);
        }
        
        [Fact]
        private void AvoidsHeldSubscribersIfKeyIsNotPressed()
        {
            var backstage = SetupBackstage();
            backstage.ProcessLogicFrame(4.2);
            Assert.Equal(0, backstage.ActionHeldCallCount);
            Assert.Equal(0, backstage.ActionHeldCallSum);
            Assert.Equal(0, backstage.ActionHeldWithDoubleCallSum);
            Assert.Equal(Vector2.Zero, backstage.ActionHeldWithVectorCallSum);
        }
        
        [Fact]
        private void AvoidsHeldSubscribersIfKeyIsReleased()
        {
            var backstage = SetupBackstage();
            backstage.Keyboard.SendKeyDown(Key.Number0);
            backstage.Keyboard.SendKeyDown(Key.Number1);
            backstage.Keyboard.SendKeyDown(Key.Number2);
            backstage.Keyboard.SendKeyUp(Key.Number0);
            backstage.Keyboard.SendKeyUp(Key.Number1);
            backstage.Keyboard.SendKeyUp(Key.Number2);
            backstage.ProcessLogicFrame(4.2);
            Assert.Equal(0, backstage.ActionHeldCallCount);
            Assert.Equal(0, backstage.ActionHeldCallSum);
            Assert.Equal(0, backstage.ActionHeldWithDoubleCallSum);
            Assert.Equal(Vector2.Zero, backstage.ActionHeldWithVectorCallSum);
        }
        
        [Fact]
        private void NotifiesSubscribersOnInputHeldEvent()
        {
            var backstage = SetupBackstage();
            backstage.Keyboard.SendKeyDown(Key.Number0);
            backstage.ProcessLogicFrame(4.2);
            Assert.Equal(1, backstage.ActionHeldCallCount);
            Assert.Equal(4.2, backstage.ActionHeldCallSum);
            Assert.Equal(0, backstage.ActionHeldWithDoubleCallSum);
            Assert.Equal(Vector2.Zero, backstage.ActionHeldWithVectorCallSum);
        }
        
        [Fact]
        private void NotifiesSubscribersOnInputHeldEventWithDouble()
        {
            var backstage = SetupBackstage();
            backstage.Keyboard.SendKeyDown(Key.Number1);
            backstage.ProcessLogicFrame(4.2);
            Assert.Equal(0, backstage.ActionHeldCallCount);
            Assert.Equal(1, backstage.ActionHeldWithDoubleCallSum);
            Assert.Equal(Vector2.Zero, backstage.ActionHeldWithVectorCallSum);
        }
        
        [Fact]
        private void NotifiesSubscribersOnInputHeldEventWithVector()
        {
            var backstage = SetupBackstage();
            backstage.Keyboard.SendKeyDown(Key.Number2);
            backstage.ProcessLogicFrame(4.2);
            Assert.Equal(0, backstage.ActionHeldCallCount);
            Assert.Equal(0, backstage.ActionHeldWithDoubleCallSum);
            Assert.Equal(new Vector2(1, 2), backstage.ActionHeldWithVectorCallSum);
        }
        
        [Fact]
        private void MultiAction_InvokesCallbackOnlyOnce()
        {
            var backstage = SetupBackstage();
            backstage.Keyboard.SendKeyDown(Key.Up);
            backstage.Keyboard.SendKeyDown(Key.Down);
            backstage.Keyboard.SendKeyDown(Key.Left);
            backstage.Keyboard.SendKeyDown(Key.Right);
            backstage.ProcessLogicFrame(4.2);
            Assert.Equal(1, backstage.MultiActionWithVectorCallCount);
            Assert.Equal(Vector2.Zero, backstage.MultiActionWithVectorCallSum);
        }
        
        [Fact]
        private void MultiAction_ProcessesIndividualPresses()
        {
            var backstage = SetupBackstage();
            backstage.Keyboard.SendKeyDown(Key.Up);
            backstage.ProcessLogicFrame(4.2);
            backstage.Keyboard.SendKeyUp(Key.Up);
            Assert.Equal(1, backstage.MultiActionWithVectorCallCount);
            Assert.Equal(new Vector2(0.0, 1.0), backstage.MultiActionWithVectorCallSum);

            backstage.MultiActionWithVectorCallCount = 0;
            backstage.MultiActionWithVectorCallSum = Vector2.Zero;
            
            backstage.Keyboard.SendKeyDown(Key.Down);
            backstage.ProcessLogicFrame(4.2);
            backstage.Keyboard.SendKeyUp(Key.Down);
            Assert.Equal(1, backstage.MultiActionWithVectorCallCount);
            Assert.Equal(new Vector2(0.0, -1.0), backstage.MultiActionWithVectorCallSum);
            
            backstage.MultiActionWithVectorCallCount = 0;
            backstage.MultiActionWithVectorCallSum = Vector2.Zero;
            
            backstage.Keyboard.SendKeyDown(Key.Left);
            backstage.ProcessLogicFrame(4.2);
            backstage.Keyboard.SendKeyUp(Key.Left);
            Assert.Equal(1, backstage.MultiActionWithVectorCallCount);
            Assert.Equal(new Vector2(-1.0, 0.0), backstage.MultiActionWithVectorCallSum);
            
            backstage.MultiActionWithVectorCallCount = 0;
            backstage.MultiActionWithVectorCallSum = Vector2.Zero;
            
            backstage.Keyboard.SendKeyDown(Key.Right);
            backstage.ProcessLogicFrame(4.2);
            backstage.Keyboard.SendKeyUp(Key.Right);
            Assert.Equal(1, backstage.MultiActionWithVectorCallCount);
            Assert.Equal(new Vector2(1.0, 0.0), backstage.MultiActionWithVectorCallSum);
        }
        
        [Fact]
        private void MultiAction_SumsValues()
        {
            var backstage = SetupBackstage();
            
            backstage.Keyboard.SendKeyDown(Key.Up);
            backstage.Keyboard.SendKeyDown(Key.Right);
            backstage.ProcessLogicFrame(4.2);
            backstage.Keyboard.SendKeyUp(Key.Up);
            backstage.Keyboard.SendKeyUp(Key.Right);
            Assert.Equal(1, backstage.MultiActionWithVectorCallCount);
            Assert.Equal(new Vector2(1.0, 1.0), backstage.MultiActionWithVectorCallSum);
            
            backstage.MultiActionWithVectorCallCount = 0;
            backstage.MultiActionWithVectorCallSum = Vector2.Zero;
            
            backstage.Keyboard.SendKeyDown(Key.Left);
            backstage.Keyboard.SendKeyDown(Key.Down);
            backstage.ProcessLogicFrame(4.2);
            backstage.Keyboard.SendKeyUp(Key.Left);
            backstage.Keyboard.SendKeyUp(Key.Down);
            Assert.Equal(1, backstage.MultiActionWithVectorCallCount);
            Assert.Equal(new Vector2(-1.0, -1.0), backstage.MultiActionWithVectorCallSum);
        }
        
        [Fact]
        private void MultiAction_AllowsAlternativeKeysInContext()
        {
            var backstage = SetupBackstage();
            
            backstage.Keyboard.SendKeyDown(Key.W);
            backstage.Keyboard.SendKeyDown(Key.D);
            backstage.ProcessLogicFrame(4.2);
            backstage.Keyboard.SendKeyUp(Key.W);
            backstage.Keyboard.SendKeyUp(Key.D);
            Assert.Equal(1, backstage.MultiActionWithVectorCallCount);
            Assert.Equal(new Vector2(1.0, 1.0), backstage.MultiActionWithVectorCallSum);
            
            backstage.MultiActionWithVectorCallCount = 0;
            backstage.MultiActionWithVectorCallSum = Vector2.Zero;
            
            backstage.Keyboard.SendKeyDown(Key.A);
            backstage.Keyboard.SendKeyDown(Key.S);
            backstage.ProcessLogicFrame(4.2);
            backstage.Keyboard.SendKeyUp(Key.A);
            backstage.Keyboard.SendKeyUp(Key.S);
            Assert.Equal(1, backstage.MultiActionWithVectorCallCount);
            Assert.Equal(new Vector2(-1.0, -1.0), backstage.MultiActionWithVectorCallSum);
        }
    }
}