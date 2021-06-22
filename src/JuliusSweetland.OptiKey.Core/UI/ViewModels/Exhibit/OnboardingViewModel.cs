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

        //private IPageViewModel _currentPageViewModel;
        private List<IPageViewModel> _pageViewModels;
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
            SetProperty(ref _pageNumber, 0);
            //CurrentPageViewModel = PageViewModels[_pageNumber];
        }

        #region Properties         

        public List<IPageViewModel> PageViewModels
        {
            get
            {
                if (_pageViewModels == null)
                    _pageViewModels = new List<IPageViewModel>();

                return _pageViewModels;
            }
        }

        public IPageViewModel CurrentPageViewModel
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
            RaisePropertyChanged("CurrentPageViewModel");
        }

        public void PrevPage()
        {
            _pageNumber--;
            _pageNumber += PageViewModels.Count;
            _pageNumber %= PageViewModels.Count;
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
