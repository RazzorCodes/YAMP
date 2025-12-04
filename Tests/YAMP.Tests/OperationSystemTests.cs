using System;
using System.Collections.Generic;
using NUnit.Framework;
using YAMP.OperationSystem.Core;

namespace YAMP.Tests
{
    [TestFixture]
    public class OperationSystemTests
    {
        private OperationExecutor _executor;
        private OperationContext _context;

        [SetUp]
        public void Setup()
        {
            _executor = new OperationExecutor();
            _context = new OperationContext
            {
                OperationName = "TestOperation",
                Arguments = new object[] { "arg1", 123 },
                State = new Dictionary<string, object>()
            };
        }

        [Test]
        public void Execute_SuccessfulPipeline_RunsAllHooks()
        {
            // Arrange
            var op = new TestOperation(true);

            // Act
            var result = _executor.Execute(op, _context);

            // Assert
            Assert.IsTrue(result.Success);
            Assert.IsTrue(op.PreHookCalled);
            Assert.IsTrue(op.ExecuteCalled);
            Assert.IsTrue(op.PostHookCalled);
            Assert.IsTrue(op.CleanupCalled);
            
            // Verify trace
            Assert.AreEqual(3, op.Trace.Count); // Pre, Execute, Post
            Assert.AreEqual("PreHook", op.Trace[0].hookName);
            Assert.AreEqual("Execute", op.Trace[1].hookName);
            Assert.AreEqual("PostHook", op.Trace[2].hookName);
        }

        [Test]
        public void Execute_PreHookFailure_AbortsPipeline()
        {
            // Arrange
            var op = new TestOperation(true);
            op.ShouldFailPreHook = true;

            // Act
            var result = _executor.Execute(op, _context);

            // Assert
            Assert.IsFalse(op.ExecuteCalled);
            Assert.IsFalse(op.PostHookCalled);
            Assert.IsTrue(op.CleanupCalled);
            
            // Verify trace
            Assert.AreEqual(1, op.Trace.Count);
            Assert.AreEqual("PreHook", op.Trace[0].hookName);
            Assert.IsFalse(op.Trace[0].success);
        }

        [Test]
        public void Execute_ExecuteFailure_SkipsPostHooks()
        {
            // Arrange
            var op = new TestOperation(false); // Execute fails

            // Act
            var result = _executor.Execute(op, _context);

            // Assert
            Assert.IsFalse(result.Success);
            Assert.IsTrue(op.PreHookCalled);
            Assert.IsTrue(op.ExecuteCalled);
            Assert.IsFalse(op.PostHookCalled); // Skipped
            Assert.IsTrue(op.CleanupCalled);
        }

        [Test]
        public void Execute_StatePassing_Works()
        {
            // Arrange
            var op = new TestOperation(true);
            
            // Act
            _executor.Execute(op, _context);

            // Assert
            Assert.IsTrue(_context.HasState("PreHookSet"));
            Assert.AreEqual("Value", _context.GetState<string>("PreHookSet"));
        }

        // Mock Operation
        private class TestOperation : IOperation
        {
            public string Name => "TestOperation";
            public bool ShouldFailPreHook = false;
            public bool ExecuteSuccess = true;

            public bool PreHookCalled = false;
            public bool ExecuteCalled = false;
            public bool PostHookCalled = false;
            public bool CleanupCalled = false;

            public List<(string hookName, bool success)> Trace;

            public TestOperation(bool executeSuccess)
            {
                ExecuteSuccess = executeSuccess;
            }

            public List<(string name, PreHook hook)> PreHooks => new List<(string name, PreHook hook)>
            {
                ("PreHook", (ref OperationContext ctx) => 
                {
                    PreHookCalled = true;
                    ctx.SetState("PreHookSet", "Value");
                    return !ShouldFailPreHook;
                })
            };

            public List<(string name, PostHook hook)> PostHooks => new List<(string name, PostHook hook)>
            {
                ("PostHook", (ref OperationContext ctx, ref OperationResult res) => 
                {
                    PostHookCalled = true;
                    return true;
                })
            };

            public List<(string name, CleanupHook hook)> Cleanup => new List<(string name, CleanupHook hook)>
            {
                ("Cleanup", (ref OperationContext ctx, ref OperationResult res, List<(string hookName, bool success)> trace) => 
                {
                    CleanupCalled = true;
                    Trace = trace;
                })
            };

            public bool Execute(ref OperationContext context, ref OperationResult result)
            {
                ExecuteCalled = true;
                result.Success = ExecuteSuccess;
                return ExecuteSuccess;
            }
        }
    }
}
