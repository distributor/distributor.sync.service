namespace Upsmile.Sync.Initiator
{
    partial class ProjectInstaller
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
            this.USInWinServiceInstaller = new System.ServiceProcess.ServiceInstaller();
            // 
            // USExWinServiceProcessInstaller
            // 
            this.USExWinServiceProcessInstaller.Account = System.ServiceProcess.ServiceAccount.LocalService;
            this.USExWinServiceProcessInstaller.Password = null;
            this.USExWinServiceProcessInstaller.Username = null;
            // 
            // USInWinServiceInstaller
            // 
            this.USInWinServiceInstaller.Description = "Win-сервис управляющий синхронизацией справочников";
            this.USInWinServiceInstaller.ServiceName = "USInWinService";
            this.USInWinServiceInstaller.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
            // 
            // ProjectInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.USExWinServiceProcessInstaller,
            this.USInWinServiceInstaller});

        }

        #endregion

        private System.ServiceProcess.ServiceProcessInstaller USExWinServiceProcessInstaller;
        private System.ServiceProcess.ServiceInstaller USInWinServiceInstaller;
    }
}