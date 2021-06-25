using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace JuliusSweetland.OptiKey.UI.ViewModels.Exhibit
{
    class OnboardingViewModel : BindableBase
    {

        #region Fields

        //private PageViewModel _currentPageViewModel;
        private List<PageViewModel> _pageViewModels;
        private int _pageNumber;
        
        #endregion

        public OnboardingViewModel()
        {
            // Add available pages
            PageViewModels.Add(new IntroViewModel());
            PageViewModels.Add(new FaceViewModel());
            PageViewModels.Add(new TobiiViewModel());
            PageViewModels.Add(new PostCalibViewModel());

            // Set starting page
            SetPage(0);
            //CurrentPageViewModel = PageViewModels[_pageNumber];
        }

        #region Properties         

        public List<PageViewModel> PageViewModels
        {
            get
            {
                if (_pageViewModels == null)
                    _pageViewModels = new List<PageViewModel>();

                return _pageViewModels;
            }
        }

        public PageViewModel CurrentPageViewModel
        {
            get
            {
                return PageViewModels[_pageNumber];
            }            
        }

        public int PageNumber
        {
            get
            {
                return _pageNumber;
            }
            set
            {
                SetProperty(ref _pageNumber, value);
            }
        }

        public int NumPages
        {
            get
            {
                return PageViewModels.Count;
            }
        }

        #endregion

        #region Methods

        public void Reset()
        {
            SetPage(0);
        }

        public bool NextPage()
        {
            int i = _pageNumber+1;
            if (i < PageViewModels.Count)
            {
                SetPage(i, true);
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool PrevPage()
        {
            int i = _pageNumber-1;
            if (i >= 0)
            {
                SetPage(i);
                return true;
            }
            else
            {
                return false;
            }            
        }

        private void SetPage(int i, bool teardownCurrent=false)
        {
            if (teardownCurrent) //TOO: distinguish between "stuff to do on finishing page" and "stuff to do whenever closed" e.g. prev vs next
            {
                // cleanly leave current page
                PageViewModels[_pageNumber].TearDown();
            }

            // set up new page
            _pageNumber = i;
            PageViewModels[_pageNumber].SetUp();

            RaisePropertyChanged("CurrentPageViewModel");                        
        }

        /*private void ChangeViewModel(IPageViewModel viewModel)
        {
            if (!PageViewModels.Contains(viewModel))
                PageViewModels.Add(viewModel);

            CurrentPageViewModel = PageViewModels
                .FirstOrDefault(vm => vm == viewModel);
        }*/

        #endregion
    }
}
