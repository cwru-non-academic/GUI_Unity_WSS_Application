$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$path=Join-Path $PSScriptRoot '../../workflows/promote-integration.yml'
$text=[IO.File]::ReadAllText($path)
function Assert-PromotionToolingWorkflow([string]$Text) {
    $checkout='(?ms)^      - name: Check out trusted wrapper tools; never execute the caller-nominated merge source\r?\n        id: promotion_tools\r?\n        uses: actions/checkout@v4\r?\n        with:\r?\n          repository: cwru-non-academic/Unity_WSS_Integration_Lib\r?\n          ref: [a-f0-9]{40}\r?\n          path: promotion-tools\r?\n          token: \$\{\{ steps.read.outputs.token \}\}\r?\n          persist-credentials: false\r?\n'
    if(@([regex]::Matches($Text,$checkout)).Count -ne 1 -or @([regex]::Matches($Text,'(?m)^        id: promotion_tools\r?$')).Count -ne 1){throw 'One immutable registered tooling checkout is required.'}
    $calls=@([regex]::Matches($Text,'(?m)^.*promotion-tools/\.github/scripts/(Prepare|Publish)-UnityPromotion\.ps1[^\r\n]*'))
    if($calls.Count -ne 3 -or @($calls|Where-Object {$_.Groups[1].Value -ceq 'Prepare'}).Count -ne 2){throw 'Validation, preparation and publication must use the executing tooling.'}
    foreach($call in $calls){
        if(!$call.Value.Contains('-ToolSha ''${{ steps.promotion_tools.outputs.commit }}''') -or !$call.Value.Contains('-RepositoryRoot "$env:GITHUB_WORKSPACE/source"')){throw 'All promotion entrypoints must bind the checkout commit output and fixed target root.'}
    }
    if(@([regex]::Matches($Text,'steps\.promotion_tools\.outputs\.commit')).Count -ne 3 -or @([regex]::Matches($Text,'-ToolSha')).Count -ne 3){throw 'Do not duplicate or redirect the executable tooling identity.'}
    $inputs=$Text.Substring(0,$Text.IndexOf("`nenv:"))
    if($inputs -match '(?i)toolsha|tool_sha|tooling|tool_root|config_path'){throw 'Dispatch inputs cannot select executable tooling.'}
}
Assert-PromotionToolingWorkflow $text
$count=1
$mutations=@(
    @('id: promotion_tools','id: caller_tools'),
    @('        id: promotion_tools',''),
    @('steps.promotion_tools.outputs.commit','steps.promotion_tools.outputs.missing'),
    @('ref: [a-f0-9]{40}','ref: ${{ inputs.wrapper_sha }}'),
    @('path: promotion-tools','path: source'),
    @('steps.promotion_tools.outputs.commit','inputs.wrapper_sha'),
    @('steps.promotion_tools.outputs.commit','steps.other.outputs.commit'),
    @('Prepare-UnityPromotion.ps1','Other-UnityPromotion.ps1'),
    @('Publish-UnityPromotion.ps1','Other-UnityPromotion.ps1'),
    @('persist-credentials: false','persist-credentials: true'),
    @('wrapper_pr: \{required: true, type: string\}','tool_sha: {required: true, type: string}')
)
foreach($mutation in $mutations){
    $bad=if($mutation[0] -in @('ref: [a-f0-9]{40}','wrapper_pr: \{required: true, type: string\}')){[regex]::Replace($text,$mutation[0],$mutation[1])}else{$text.Replace($mutation[0],$mutation[1])}
    if($bad -ceq $text){throw 'Mutation did not apply.'}
    $rejected=$false;try{Assert-PromotionToolingWorkflow $bad}catch{$rejected=$true}
    if(!$rejected){throw "Bad caller binding accepted: $($mutation[0])"};$count++
}
# A single miswired phase must fail even if the other two remain correct.
$needle='steps.promotion_tools.outputs.commit'
$at=$text.IndexOf($needle)
$bad=$text.Substring(0,$at)+'inputs.wrapper_sha'+$text.Substring($at+$needle.Length)
$rejected=$false;try{Assert-PromotionToolingWorkflow $bad}catch{$rejected=$true}
if(!$rejected){throw 'One inconsistent phase was accepted.'};$count++
Write-Output "PASS: $count GUI caller-tooling binding checks; no dispatch or runtime execution."
