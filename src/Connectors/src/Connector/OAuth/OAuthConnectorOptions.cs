// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common;
using Steeltoe.Common.Configuration;

namespace Steeltoe.Connector.OAuth;

public class OAuthConnectorOptions : AbstractServiceConnectorOptions
{
    private const string SecurityClientSectionPrefix = "security:oauth2:client";
    private const string SecurityResourceSectionPrefix = "security:oauth2:resource";

    public string OAuthServiceUrl { get; set; } = OAuthConnectorDefaults.DefaultOAuthServiceUrl;

    public string ClientId { get; set; } = OAuthConnectorDefaults.DefaultClientId;

    public string ClientSecret { get; set; } = OAuthConnectorDefaults.DefaultClientSecret;

    public string UserAuthorizationUri { get; set; } = OAuthConnectorDefaults.DefaultAuthorizationUri;

    public string AccessTokenUri { get; set; } = OAuthConnectorDefaults.DefaultAccessTokenUri;

    public string UserInfoUri { get; set; } = OAuthConnectorDefaults.DefaultUserInfoUri;

    public string TokenInfoUri { get; set; } = OAuthConnectorDefaults.DefaultCheckTokenUri;

    public string JwtKeyUri { get; set; } = OAuthConnectorDefaults.DefaultJwtTokenKey;

    public List<string> Scope { get; set; }

    public bool ValidateCertificates { get; set; } = OAuthConnectorDefaults.DefaultValidateCertificates;

    public OAuthConnectorOptions()
    {
    }

    public OAuthConnectorOptions(IConfiguration configuration)
    {
        ArgumentGuard.NotNull(configuration);

        IConfigurationSection section = configuration.GetSection(SecurityClientSectionPrefix);
        section.Bind(this);
        ValidateCertificates = GetCertificateValidation(section, configuration, ValidateCertificates);

        section = configuration.GetSection(SecurityResourceSectionPrefix);
        section.Bind(this);
    }

    private static bool GetCertificateValidation(IConfigurationSection configurationSection, IConfiguration resolve, bool def)
    {
        return ConfigurationValuesHelper.GetBoolean("validate_certificates", configurationSection, resolve, def);
    }
}
