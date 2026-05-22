using '../main.bicep'

param environmentName = 'qa'
param location = 'southafricanorth'

param sqlAdminObjectId = '<REPLACE_WITH_ENTRA_OBJECT_ID>'
param sqlAdminLogin = '<REPLACE_WITH_ENTRA_UPN_OR_GROUP_NAME>'

param logRetentionDays = 30
