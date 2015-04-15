using FirstFloor.ModernUI.Windows.Controls;
using System;
using System.ComponentModel;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading;
using AerSpeech;
using System.Speech;
using System.Speech.Recognition;

namespace AerWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : ModernWindow
    {
        bool _RunWorker;
        Thread _Worker;

        public MainWindow()
        {
            InitializeComponent();

            _RunWorker = true;
            _Worker = new Thread(ExecuteThread);
            _Worker.Start();
            this.Closing += KillThread;
            

        }

        public void ExecuteThread()
        {
            AerDB data = new AerDB(@"json\");
            AerTalk talk = new AerTalk();

            Personality person = new Personality(talk, data);
            AerHandler handler = new AerHandler(data, person);
            //I know this is bad, but there's no good way to get the delegate surfaced out of AerInput in to AerTalk yet.
            // This could be solved with a service registry, but I haven't thought that through yet
            AerInput input = new AerInput(@"Grammars\", person.GrammarLoaded_Handler); 

            while (_RunWorker)
            {
                if (input.NewInput)
                {
                    input.NewInput = false;
                    handler.InputHandler(input.LastResult);
                }
                Thread.Sleep(10); //Keep CPU usage down until we handle responses async
            }
        }

        //Causes vshost to crash - wut
        public void KillThread(object sender, CancelEventArgs e)
        {
            _RunWorker = false;
            _Worker.Join(); 
        }
    }


}
