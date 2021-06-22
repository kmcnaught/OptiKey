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

        #endregion

        #region Methods
        
        public void NextPage()
        {            
            _pageNumber++;
            _pageNumber %= PageViewModels.Count;
            SetPage(_pageNumber);
        }

        public void PrevPage()
        {
            _pageNumber--;
            _pageNumber += PageViewModels.Count;
            _pageNumber %= PageViewModels.Count;
            SetPage(_pageNumber);
        }

        private void SetPage(int i)
        {
            if (_pageNumber > 0) 
                PageViewModels[_pageNumber-1].TearDown();
            RaisePropertyChanged("CurrentPageViewModel");            
            PageViewModels[_pageNumber].SetUp();
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
