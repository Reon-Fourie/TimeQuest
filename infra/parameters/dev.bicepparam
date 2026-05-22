using '../main.bicep'

param environmentName = 'dev'
param location = 'southafricanorth'

// Replace with the Entra ID Object ID of the developer/group that will be the SQL admin.
// Get it with: az ad user show --id <upn> --query id -o tsv
param sqlAdminObjectId = '<REPLACE_WITH_ENTRA_OBJECT_ID>'
param sqlAdminLogin = '<REPLACE_WITH_ENTRA_UPN_OR_GROUP_NAME>'

// 30 days for dev — cheaper log storage
param logRetentionDays = 30
