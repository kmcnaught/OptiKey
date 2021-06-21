using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using WPFMediaKit.DirectShow.Controls;

namespace JuliusSweetland.OptiKey.UI.Views.Exhibit
{
    /// <summary>
    /// Interaction logic for FaceWindow.xaml
    /// </summary>
    public partial class FaceWindow : Window
    {
        public FaceWindow()
        {
            InitializeComponent();
            // Use first video source
            videoCapElement.VideoCaptureDevice = MultimediaUtil.VideoInputDevices[0];
        }
    }
}
