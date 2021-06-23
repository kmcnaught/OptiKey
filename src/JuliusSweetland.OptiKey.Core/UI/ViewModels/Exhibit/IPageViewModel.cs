using Prism.Mvvm;
using System;

namespace JuliusSweetland.OptiKey.UI.ViewModels.Exhibit
{
    abstract public class PageViewModel : BindableBase
    {
        abstract public void SetUp();
        abstract public void TearDown();

        protected DateTime initTime;

        protected void SetInitTime()
        {
            initTime = DateTime.Now;
        }

        protected bool canGoForward = false;
        public bool CanGoForward
        {
            get { return canGoForward; }
            set { SetProperty(ref canGoForward, value); }
        }

        protected bool canGoBackward = false;
        public bool CanGoBackward
        {
            get { return canGoBackward; }
            set { SetProperty(ref canGoBackward, value); }
        }
    }
}
