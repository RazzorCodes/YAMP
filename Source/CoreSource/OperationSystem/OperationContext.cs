using System.Collections.Generic;

namespace YAMP.OperationSystem.Core
{
    /// <summary>
    /// Mutable operation context that can be modified by hooks.
    /// Generic design with object[] arguments makes it RimWorld-agnostic.
    /// </summary>
    public class OperationContext
    {
        public string OperationName { get; set; }
        public object[] Arguments { get; set; }
        public Dictionary<string, object> State { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Get strongly-typed argument at index.
        /// </summary>
        public T GetArgument<T>(int index)
        {
            if (Arguments == null || index < 0 || index >= Arguments.Length)
                return default(T);
            return (T)Arguments[index];
        }

        /// <summary>
        /// Set state value for hook communication.
        /// </summary>
        public void SetState(string key, object value)
        {
            State[key] = value;
        }

        /// <summary>
        /// Get strongly-typed state value.
        /// </summary>
        public T GetState<T>(string key)
        {
            return State.ContainsKey(key) ? (T)State[key] : default(T);
        }

        /// <summary>
        /// Check if state key exists.
        /// </summary>
        public bool HasState(string key)
        {
            return State.ContainsKey(key);
        }
    }
}
