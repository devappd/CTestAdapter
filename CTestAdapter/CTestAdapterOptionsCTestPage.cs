using System.ComponentModel;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.Shell;

namespace CTestAdapter
{
  public class CTestAdapterOptionsCTestPage : DialogPage
  {
    private bool _changed = false;
    private readonly CTestAdapterPackage _package;

    private string _ctestRunArguments = "";

    public CTestAdapterOptionsCTestPage()
    {
      this._package = CTestAdapterPackage.Instance;
    }

    [Category("Execution")]
    [DisplayName("CTest Arguments")]
    [Description("Extra arguments to pass to the 'ctest' command when running a test. Do not specify -R and -C.")]
    public string CTestRunArguments
    {
      get { return this._ctestRunArguments; }
      set
      {
        if (value == this._ctestRunArguments)
        {
          return;
        }
        this._ctestRunArguments = value;
        this._changed = true;
      }
    }

    protected override void OnApply(PageApplyEventArgs e)
    {
      if (!this._changed || null == this._package)
      {
        return;
      }
      this._package.SetCTestOptions(this);
      this._changed = false;
      base.OnApply(e);
    }

    protected override void OnActivate(CancelEventArgs e)
    {
      base.OnActivate(e);
      this.LoadSettingsFromStorage();
    }
  }
}
