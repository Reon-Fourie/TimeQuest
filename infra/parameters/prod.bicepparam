using '../main.bicep'

param environmentName = 'prod'
param location = 'southafricanorth'

param sqlAdminObjectId = '<REPLACE_WITH_ENTRA_OBJECT_ID>'
param sqlAdminLogin = '<REPLACE_WITH_ENTRA_UPN_OR_GROUP_NAME>'

// 90 days hot — remainder exported to cold storage per 7-year audit retention requirement
param logRetentionDays = 90
