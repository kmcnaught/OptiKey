using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JuliusSweetland.OptiKey.UI.ViewModels.Exhibit
{
    class EyeTrackerErrorViewModel : PageViewModel
    {
        protected string errorString = "";
        public string ErrorString
        {
            get { return errorString; }
            set
            {
                errorString = value;
                RaisePropertyChanged("ErrorString");
            }
        }
    }
}
