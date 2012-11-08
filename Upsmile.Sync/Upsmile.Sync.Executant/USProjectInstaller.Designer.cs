namespace Upsmile.Sync.Executant
{
    partial class ExWinProjectInstaller
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.USExWinServiceProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
            this.USExWinServiceInstaller = new System.ServiceProcess.ServiceInstaller();
            // 
            // USExWinServiceProcessInstaller
            // 
            this.USExWinServiceProcessInstaller.Account = System.ServiceProcess.ServiceAccount.LocalService;
            this.USExWinServiceProcessInstaller.Password = null;
            this.USExWinServiceProcessInstaller.Username = null;
            // 
            // USExWinServiceInstaller
            // 
            this.USExWinServiceInstaller.Description = "Win-сервис управляющий запуском WCF-сервисом синхронизации справочников";
            this.USExWinServiceInstaller.ServiceName = "USExWinService";
            this.USExWinServiceInstaller.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
            // 
            // ExWinProjectInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.USExWinServiceInstaller,
            this.USExWinServiceProcessInstaller});

        }

        #endregion

        private System.ServiceProcess.ServiceInstaller USExWinServiceInstaller;
        public System.ServiceProcess.ServiceProcessInstaller USExWinServiceProcessInstaller;
    }
}