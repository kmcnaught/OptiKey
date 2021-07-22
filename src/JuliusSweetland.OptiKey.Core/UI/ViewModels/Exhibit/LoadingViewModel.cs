using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JuliusSweetland.OptiKey.UI.ViewModels.Exhibit
{
    class LoadingViewModel : PageViewModel
    {
        public LoadingViewModel()
        {
            CanGoBackward = false;
            CanGoForward = true;
        }

        public override void SetUp()
        {
            SetInitTime();
        }

        public override void TearDown()
        {
        }
    }
}
