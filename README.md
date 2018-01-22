Before you start creating the project there are few things that you need to make a note of. 

1. You should have a valid APPSeCONNECT Account.
2. You must have requested for ISV partner account for APPSeCONNECT using http://insync.co.in/isv/. This partnership will give you access to http://admin.appseconnect.com.
3. You have defined the entities for your app which you want to communicate. 
4. You know at least one of the .NET langauges such that you can code on the platform. 

Adapter API

Adapter in APPSeCONNECT is used to communicate one APP. The main idea behind creating an API is to create an interface between the Application it wants to connect to with the platform. The Adapter will fetch data to and fro from the application and give it back to the Agent. 

Interfaces : 

1. IAdapter : This interface represents the entry point of the Adapter. The Execute is the main entry point to communicate with the Agent. The Agent calls the Execute function to Pull / Push data. It passes on ExecuteSettings which allows the adapter to get configuration details. The Initialize will get the ApplicationContext object which can be used to get all the contextual data.
2. IAppResource : Here you would place all the special methods that you want to reference during data transformation.
3. IPageView : The PageView will be used as an entrypoint of the credential window.

For more details, do check our documentation at : 
http://support.appseconnect.com/support/solutions/articles/4000049687

Also follow the SDK API references from :

http://isdn.appseconnect.com/


Also for further support drop us a mail at support@insync.co.in.

Thank you.
