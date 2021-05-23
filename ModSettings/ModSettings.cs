using System;
using System.Collections.Generic;
using System.Text;
using VoxelTycoon;

namespace ModSettings
{
    abstract public class ModSettings<T> where T: ModSettings<T>, new()
    {
        private Action _settingsChanged;
        protected ModSettings()
        {
            this.Behaviour = UpdateBehaviour.Create(typeof(T).Name);
            this.Behaviour.OnDestroyAction = delegate ()
            {
                this.OnDeinitialize();
                ModSettings<T>._current = default(T);
            };
            this.OnInitialize();
        }

        public static T Current
        {
            get
            {
                T result;
                if ((result = ModSettings<T>._current) == null)
                {
                    result = (ModSettings<T>._current = Activator.CreateInstance<T>());
                }
                return result;
            }
        }

        private protected UpdateBehaviour Behaviour { get; private set; }

        protected virtual void OnInitialize()
        {
        }

        protected virtual void OnDeinitialize()
        {
        }

        public void Subscribe(Action settingsChanged)
        {
            _settingsChanged += settingsChanged;
        }

        public void Unsubscribe(Action settingsChanged)
        {
            _settingsChanged -= settingsChanged;
        }

        protected virtual void OnChange()
        {
            _settingsChanged?.Invoke();
        }

        private static T _current;

    }
}
