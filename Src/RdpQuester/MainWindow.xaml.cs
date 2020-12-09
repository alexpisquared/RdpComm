using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace RdpQuester
{
  public partial class MainWindow : AAV.WPF.Base.WindowBase
  {
    const string _ext = ".rdc", _questPending = "`", _appstg = @"RdpQuester.appstg.json";
    readonly string _rqstFile;
    readonly string _path = @"C:\temp\";
    string _ansr = "·";
    int _patienceInSec = 20;
    readonly TimeSpan _pollingPeriod = new TimeSpan(0, 5, 0);

    static DateTimeOffset Eta(DateTimeOffset tn)
    {
      var cm = tn.Minute > 2 ? tn.Minute : tn.Minute + 60;
      var nm = (cm) + (5 - ((cm - 2) % 5)) + (tn.Minute > 2 ? 0 : -60) + .33;
      var eta = new DateTime(tn.Year, tn.Month, tn.Day, tn.Hour, 0, 0).AddMinutes(nm);
      Debug.WriteLine($"{tn.Minute,3}   {cm,3}   { nm,3}     {tn}   {eta}");
      return eta;
    }

    public MainWindow()
    {
      //var tn = new DateTimeOffset(2020, 7, 16, 23, 0, 22);      for (int i = 0; i < 5; i++)   Eta(tn.AddMinutes(i));      for (int i = 55; i < 60; i++) Eta(tn.AddMinutes(i));

      InitializeComponent();
      pnlThanks.Visibility = Visibility.Hidden;

      if (File.Exists(_appstg))
        _path = File.ReadAllText(_appstg);
#if DEBUG
      else
        File.WriteAllText(_appstg, _path);
#endif

      _rqstFile = _path + _questPending + _ext;      //_resp = _path + "~" + _ext;
    }

    void Window_Loaded(object s, RoutedEventArgs re)
    {
      var now = DateTimeOffset.Now;
      if (File.Exists(_rqstFile))
      {
        var fi = new FileInfo(_rqstFile);
        if ((fi.CreationTime - now) < _pollingPeriod || (fi.LastWriteTime - now) < _pollingPeriod)
        {
          lblBttm.Content = $"BTW, a request already pending ... ";
        }
      }
    }

    void updateClock()
    {
      //btnDefault.Content = (--_patienceInSec) % 2 == 0 ? "Now" : $"In {_patienceInSec} sec";
      btnDefault.Content = $"Now ({--_patienceInSec})";
      if (_patienceInSec <= 0)
      {
        if (_ansr == "·")
          sendRqst(" Nobody's there - Timed out ");
      }
    }

    void onClose(object s, RoutedEventArgs e) => Application.Current.Shutdown();

    void onClick(object s, RoutedEventArgs e) => sendRqst(((Button)s).Content.ToString());

    void sendRqst(string quest)
    {
      try
      {
        switch (quest)
        {
          case "10 min":      /**/ _ansr += $" At {DateTimeOffset.Now.AddMinutes(05):HH·mm} ...or '{quest}'"; lblRqst.Content = $"I need access for {quest}"; break;
          case "1 hour":      /**/ _ansr += $" At {DateTimeOffset.Now.AddMinutes(20):HH·mm} ...or '{quest}'"; lblRqst.Content = $"I need access for {quest}"; break;
          case "Till EODay":  /**/ _ansr += $" At {DateTimeOffset.Now.AddMinutes(60):HH·mm} ...or '{quest}'"; lblRqst.Content = $"I need the box until end of day"; break;
          case "Rebooting":   /**/ _ansr += $" At {DateTimeOffset.Now.AddMinutes(60):HH·mm} ...or '{quest}'"; lblRqst.Content = $"The box needs to be rebooted"; break;
          default: _ansr += quest; break;
        }

        pnlButtns.Visibility = Visibility.Hidden;
        lblBttm.Content = "The following request has been sent:";

        foreach (var file in Directory.GetFiles(_path, $"*{_ext}")) File.Delete(file);
        File.WriteAllText(_rqstFile, $"{lblRqst.Content}");

        var now = DateTimeOffset.Now;
        _nextSched = Eta(now);

        _timer = new DispatcherTimer(TimeSpan.FromSeconds(.96), DispatcherPriority.Background, new EventHandler((s, e) => checkAndInform()), Dispatcher.CurrentDispatcher); //tu: one-line timer
      }
      catch (Exception ex) { Debug.WriteLine(ex); if (Debugger.IsAttached) Debugger.Break(); }
    }


    void checkAndInform()
    {
      var rdc = Directory.GetFiles(_path, $"*{_ext}").FirstOrDefault(f => Path.GetFileNameWithoutExtension(f) != _questPending);
      if (rdc != null)
      {
        Opacity = 1;
        _timer.Stop();
        lblBttm.Content = "The following response jsut arrived:";
        lblRqst.Content = $"{Path.GetFileNameWithoutExtension(rdc)}";
        lblFdbk.Content = $"";
        lblRqst.FontSize = 16;
        File.Delete(rdc);
      }
      else
      {
        Opacity = .7;
        _timeLeft = _nextSched - DateTimeOffset.Now;
        lblFdbk.Content = $"Expect an answer in {_timeLeft:mm\\:ss} min, at{_nextSched:HH:mm}.";
      }
    }

    TimeSpan _timeLeft;
    DateTimeOffset _nextSched = DateTimeOffset.MinValue;
    private DispatcherTimer _timer;
  }
}
