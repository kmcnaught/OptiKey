using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JuliusSweetland.OptiKey.UI.ViewModels.Exhibit
{
    class IntroViewModel : PageViewModel
    {
        public IntroViewModel()
        {
            CanGoBackward = false;
            CanGoForward = true;
        }

        public override void SetUp()
        {
        }

        public override void TearDown()
        {
        }
    }
}
