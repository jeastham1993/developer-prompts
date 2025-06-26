// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using System.Collections.Generic;
using System.Text.Json.Serialization;
using Amazon.Lambda.APIGatewayEvents;
using ContactManager.Core.ContactRegistration;

namespace ContactManager.Api;

[JsonSerializable(typeof(Contact))]
[JsonSerializable(typeof(ContactRequest))]
[JsonSerializable(typeof(ContactResponse))]
[JsonSerializable(typeof(APIGatewayHttpApiV2ProxyRequest))]
[JsonSerializable(typeof(APIGatewayHttpApiV2ProxyResponse))]
[JsonSerializable(typeof(Dictionary<string, string>))]
public partial class CustomSerializationContext : JsonSerializerContext
{
    
}