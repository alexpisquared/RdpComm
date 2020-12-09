using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace RdpCommunicator
{
  public partial class MainWindow : Window
  {
    const string _ext = ".rdc", _core = "`", _appstg = @"appstg.json";
    readonly string _rqst;
    readonly string _resp;
    readonly string _path = @"C:\temp\";
    string _ansr = "·";
    bool _allowExit = false;
    int _patienceInSec = 20;

    public MainWindow()
    {
      InitializeComponent();
      pnlThanks.Visibility = Visibility.Hidden;
      if (File.Exists(_appstg))
        _path = File.ReadAllText(_appstg);

      if (string.IsNullOrEmpty(_path) || !File.Exists(_appstg))
      {
        File.WriteAllText(_appstg, _path);
      }

      _rqst = _path + _core + _ext;
      _resp = _path + "~" + _ext;
    }

    void Window_Loaded(object sender, RoutedEventArgs re)
    {
      if (File.Exists(_rqst))
      {
        lblRqst.Content = File.ReadAllText(_rqst);
        new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Background, new EventHandler((s, e) => updateClock()), Dispatcher.CurrentDispatcher).Start();

        Opacity = 1; Visibility = Visibility.Visible; // still flickers on XP => off screen offset is the solution:
        //taken care by animation: Top = 10;
      }
      else
      {
        lblHey.Content = "No request file...";
#if DevOps
        new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Background, new EventHandler((s, e) => updateClock()), Dispatcher.CurrentDispatcher).Start();
        lblRqst.Content = "Debug non-request-checking mode";
        _allowExit = true;
#else
        App.Current.Shutdown();
#endif
      }
      //Task.Factory.StartNew(() => Thread.Sleep(1000)).ContinueWith(_ => {        pnlMain.Background = Brushes.Brown;      }, TaskScheduler.FromCurrentSynchronizationContext());
    }

    void updateClock()
    {
      //btnDefault.Content = (--_patienceInSec) % 2 == 0 ? "Now" : $"In {_patienceInSec} sec";
      btnDefault.Content = $"Now ({--_patienceInSec})";
      if (_patienceInSec <= 0)
      {
        if (_ansr == "·")
          thankAndCLose(" Nobody's there - Timed out ");
      }
    }

    protected override void OnClosing(CancelEventArgs e)
    {
      base.OnClosing(e);

      try
      {
        e.Cancel = !_allowExit;
        lblRqst.Content = _allowExit ? "OK" : "Please press the button matching your ETA";

        if (_allowExit && File.Exists(_rqst))
        {
          var newName = $"{_ansr} from {DateTime.Now:HH·mm}";
          File.Move(_rqst, _rqst.Replace(_core, newName));
#if DEBUG
          Thread.Sleep(2500);
          File.Move(_rqst.Replace(_core, newName), _rqst);
#endif
        }
      }
      catch (Exception ex) { Debug.WriteLine(ex); if (Debugger.IsAttached) Debugger.Break(); }
    }
    void onClose(object sender, RoutedEventArgs e)
    {
      _allowExit = true;
      App.Current.Shutdown();
    }

    void onClick(object sender, RoutedEventArgs e) => thankAndCLose(((Button)sender).Content.ToString());

    void thankAndCLose(string ansr)
    {
      try
      {
        _allowExit = true;
        lblHey.Visibility =
        lblRqst.Visibility =
        lblBttm.Visibility =
        pnlButtns.Visibility = Visibility.Hidden;
        pnlThanks.Visibility = Visibility.Visible;
        //File.WriteAllText(_resp, $"{DateTime.Now:MMMdd-HH:mm} + {_ansr}");

        new DispatcherTimer(TimeSpan.FromSeconds(10), DispatcherPriority.Background, new EventHandler((s, e) => { App.Current.Shutdown(); }), Dispatcher.CurrentDispatcher).Start();

        //Task.Factory.StartNew(() => Thread.Sleep(750)).ContinueWith(_ => { App.Current.Shutdown(); }, TaskScheduler.FromCurrentSynchronizationContext());

        switch (ansr)
        {
          case "In 5 min":  /**/ _ansr += $" At {DateTime.Now.AddMinutes(05):HH·mm} ...or '{ansr}'"; break;
          case "In 20 min": /**/ _ansr += $" At {DateTime.Now.AddMinutes(20):HH·mm} ...or '{ansr}'"; break;
          case "In 1 hour": /**/ _ansr += $" At {DateTime.Now.AddMinutes(60):HH·mm} ...or '{ansr}'"; break;
          default: _ansr += ansr; break;
        }
      }
      catch (Exception ex) { Debug.WriteLine(ex); if (Debugger.IsAttached) Debugger.Break(); }
    }

    public static void ExecuteAfter(Action action, int milliseconds)
    {
      System.Threading.Timer timer = null;
      timer = new System.Threading.Timer(s =>
      {
        action();
        timer.Dispose();
        lock (_timers)
          _timers.Remove(timer);
      }, null, milliseconds, uint.MaxValue - 10);
      lock (_timers)
        _timers.Add(timer);
    }

    static readonly HashSet<System.Threading.Timer> _timers = new HashSet<System.Threading.Timer>();
  }
}