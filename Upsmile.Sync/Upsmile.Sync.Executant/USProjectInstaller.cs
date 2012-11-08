using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;


namespace Upsmile.Sync.Executant
{
    [RunInstaller(true)]
    public partial class ExWinProjectInstaller : System.Configuration.Install.Installer
    {
        public ExWinProjectInstaller()
        {
            InitializeComponent();
        }
    }
}
