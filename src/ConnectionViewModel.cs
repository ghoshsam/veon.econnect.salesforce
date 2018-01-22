
using InSync.eConnect.APPSeCONNECT.Helpers;
using InSync.eConnect.APPSeCONNECT.Storage;
using InSync.eConnect.APPSeCONNECT.Utils;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Windows;
using System.Windows.Input;


namespace Veon.eConnect.Salesforce
{
    /// <summary>
    ///  Represents the ViewModel class for Credential window for the application
    /// </summary>
    public class ConnectionViewModel : ObservableObjectGeneric<ConnectionViewModel>
    {
        #region Private Variables

        private CredentialModel _credentialInfo;

        private ApplicationUtil _applicationUtils;
        private ICommand _validateCommand;
        private ICommand _saveCommand;
        private Visibility _progressBar = Visibility.Collapsed;
        private string _connectionStatus;
        private string _forecolor;
        private Logger logger;


        #endregion

        #region Public constructors
        public ConnectionViewModel()
        {
           
        }

        #endregion

        internal void Initialize(ApplicationUtil applicationUtility)
        {
            this._applicationUtils = applicationUtility;
            this.logger = applicationUtility.Logger;


            var returnDetails = applicationUtility.CredentialStore.GetConnectionDetails();

            _credentialInfo = ObjectUtils.JsonDeserialize<CredentialModel>(returnDetails.Value);
            if(_credentialInfo==null)
            {
                this._credentialInfo = new CredentialModel();
            }

        }

        #region Public Members

        public string UserName {
            get
            {
                return this._credentialInfo.UserName;

            }
            set
            {
                if (this._credentialInfo.UserName != value)
                {
                    this._credentialInfo.UserName = value;
                    this.OnPropertyChanged("UserName");
                }    

            }
        }

        public string Password
        {
            get
            {
                return this._credentialInfo.Password;

            }
            set
            {
                if (this._credentialInfo.Password != value)
                {
                    this._credentialInfo.Password = value;
                    this.OnPropertyChanged("Password");
                }

            }
        }

        public string CallBackURL
        {
            get
            {
                return this._credentialInfo.CallBackURL;

            }
            set
            {
                if (this._credentialInfo.CallBackURL != value)
                {
                    this._credentialInfo.CallBackURL = value;
                    this.OnPropertyChanged("CallBackURL");
                }

            }
        }

        public string ConsumerSecret
        {
            get
            {
                return this._credentialInfo.ConsumerSecret;

            }
            set
            {
                if (this._credentialInfo.ConsumerSecret != value)
                {
                    this._credentialInfo.ConsumerSecret = value;
                    this.OnPropertyChanged("ConsumerSecret");
                }

            }
        }

        public string ConsumerKey
        {
            get
            {
                return this._credentialInfo.ConsumerKey;

            }
            set
            {
                if (this._credentialInfo.ConsumerKey != value)
                {
                    this._credentialInfo.ConsumerKey = value;
                    this.OnPropertyChanged("ConsumerKey");
                }

            }
        }

        public string Token
        {
            get
            {
                return this._credentialInfo.Token;

            }
            set
            {
                if (this._credentialInfo.Token != value)
                {
                    this._credentialInfo.Token = value;
                    this.OnPropertyChanged("Token");
                }

            }
        }

        public string Protocol
        {
            get
            {
                return this._credentialInfo.Protocol;

            }
            set
            {
                if (this._credentialInfo.Protocol != value)
                {
                    this._credentialInfo.Protocol = value;
                    this.OnPropertyChanged("Protocol");
                }

            }
        }

        public IEnumerable<string> SecurityProtocols
        {
            get
            {
                return Enum.GetNames(typeof(SecurityProtocolType));

            }
        }

        public string ConnectionStatus
        {
            get
            {
                return _connectionStatus;
            }
            set
            {
                _connectionStatus = value;
                OnPropertyChanged("ConnectionStatus");
            }
        }

        public string ForeColor
        {
            get
            {
                return _forecolor;
            }
            set
            {
                _forecolor = value;
                OnPropertyChanged("ForeColor");
            }
        }

        public Visibility ProgressBar
        {
            get { return this._progressBar; }
            set
            {
                this._progressBar = value;
                this.OnPropertyChanged("ProgressBar");
            }
        }
        /// <summary>
        ///  Gets the Validate command
        /// </summary>
        /// <value>The validate command.</value>
        public ICommand ValidateCommand
        {
            get 
            { 
                this._validateCommand = _validateCommand ??  new RelayCommand(p => Validate(), null, false);
                return _validateCommand;
            }
        }

        /// <summary>
        ///  Gets the Save command
        /// </summary>
        /// <value>The save command.</value>
        public ICommand SaveCommand
        {
            get 
            { 
                this._saveCommand = _saveCommand ?? new RelayCommand(p => Save(), null, false);
                return this._saveCommand;
            }
        }


        #endregion
        private bool ConnectionState()
        {
            var adapter = new Adapter();
            string logindata = adapter.LoginState(this._credentialInfo);
            JObject obj = JObject.Parse(logindata);
            string token = (string)obj["access_token"];
            

            if (string.IsNullOrEmpty(token))
            {
                logger.ErrorLog("Failed to validate connection");
                return false;
            }
            else
            {
                logger.ErrorLog("SalesforceConnectionValidation");
                return true;
            }


        }

        private void Validate()
        {
            //ToDo : Validate the credentials and show message on UI.
              
         

            System.Windows.Application.Current.Dispatcher.BeginInvoke((Action)delegate()
            {
                ConnectionStatus = "";
                ForeColor = "";
                this.ProgressBar = Visibility.Visible;
            });
            bool state = this.ConnectionState();

            System.Windows.Application.Current.Dispatcher.BeginInvoke((Action)delegate()
            {
                if (state)
                {
                    this.ConnectionStatus = "Connection Validated successfully";
                    ForeColor = "Green";
                }
                else
                {
                    this.ConnectionStatus = "Test failed";
                    ForeColor = "Red";
                }

                this.ProgressBar = Visibility.Collapsed;
            });
        }


       
       
 
        private void Save()
        {
            var saveResponse = this._applicationUtils.CredentialStore.SaveConnectionDetails<CredentialModel>(this._credentialInfo);

            //ToDo : Show a message on successful save.
             System.Windows.Application.Current.Dispatcher.BeginInvoke((Action)delegate()
            {
                //ConnectionStatus = "";
                //ForeColor = "";
                this.ProgressBar = Visibility.Visible;
            });

            if (saveResponse.Value)
            {
                ConnectionStatus = "Connection saved successfully";
                ForeColor = "Green";
            }
            else
            {
                ConnectionStatus = "Connection saving failed.";
                ForeColor = "Red";
            }

            System.Windows.Application.Current.Dispatcher.BeginInvoke((Action)delegate()
            {
                this.ProgressBar = Visibility.Collapsed;
            });
        }

    }
       
}