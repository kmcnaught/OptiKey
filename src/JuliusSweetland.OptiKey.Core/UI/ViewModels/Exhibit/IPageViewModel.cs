using Prism.Mvvm;


namespace JuliusSweetland.OptiKey.UI.ViewModels.Exhibit
{
    abstract public class PageViewModel : BindableBase
    {
        abstract public void SetUp();
        abstract public void TearDown();

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
