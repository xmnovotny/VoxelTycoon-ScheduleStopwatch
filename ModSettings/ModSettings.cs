using System;
using System.Collections.Generic;
using System.Text;

namespace ModSettings
{
    abstract class ModSettings
    {
        private Action _settingsChanged;

        public void Subscribe(Action settingsChanged)
        {
            _settingsChanged += settingsChanged;
        }

        public void Unsubscribe(Action settingsChanged)
        {
            _settingsChanged -= settingsChanged;
        }

        protected void OnChange()
        {
            _settingsChanged?.Invoke();
        }

    }
}
