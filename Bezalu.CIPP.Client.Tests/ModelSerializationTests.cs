using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bezalu.CIPP.Client.Models;
using Bezalu.CIPP.Client.Tests.Infrastructure;

namespace Bezalu.CIPP.Client.Tests
{
    public class ModelSerializationTests
    {
        [Fact]
        public async Task StandardResultsSerializesResults()
        {
            var model = new StandardResults { Results = new List<string> { "Success", "Created user" } };

            var json = await KiotaTestHelpers.SerializeToJsonAsync(model);

            Assert.Contains("\"Results\":[\"Success\",\"Created user\"]", json);
        }

        [Fact]
        public async Task StandardResultsRoundTripsResults()
        {
            var original = new StandardResults { Results = new List<string> { "Success", "Created user" } };

            var json = await KiotaTestHelpers.SerializeToJsonAsync(original);
            var roundTripped = await KiotaTestHelpers.DeserializeFromJsonAsync(json, StandardResults.CreateFromDiscriminatorValue);

            Assert.NotNull(roundTripped);
            Assert.Equal(original.Results, roundTripped!.Results);
        }

        [Fact]
        public async Task LabelValueRoundTripsLabelAndValue()
        {
            var original = new LabelValue { Label = "United States", Value = "US" };

            var json = await KiotaTestHelpers.SerializeToJsonAsync(original);
            var roundTripped = await KiotaTestHelpers.DeserializeFromJsonAsync(json, LabelValue.CreateFromDiscriminatorValue);

            Assert.NotNull(roundTripped);
            Assert.Equal("United States", roundTripped!.Label);
            Assert.Equal("US", roundTripped.Value);
        }

        [Fact]
        public async Task ScheduledTaskRoundTripsEnabledFlag()
        {
            var original = new ScheduledTask { Enabled = true, Date = DateTimeOffset.Parse("2025-01-15T08:30:00Z") };

            var json = await KiotaTestHelpers.SerializeToJsonAsync(original);
            var roundTripped = await KiotaTestHelpers.DeserializeFromJsonAsync(json, ScheduledTask.CreateFromDiscriminatorValue);

            Assert.NotNull(roundTripped);
            Assert.True(roundTripped!.Enabled);
        }

        [Fact]
        public async Task ScheduledTaskRoundTripsDate()
        {
            var date = DateTimeOffset.Parse("2025-01-15T08:30:00Z");
            var original = new ScheduledTask { Enabled = true, Date = date };

            var json = await KiotaTestHelpers.SerializeToJsonAsync(original);
            var roundTripped = await KiotaTestHelpers.DeserializeFromJsonAsync(json, ScheduledTask.CreateFromDiscriminatorValue);

            Assert.NotNull(roundTripped);
            Assert.Equal(date, roundTripped!.Date);
        }

        [Fact]
        public async Task PostExecutionRoundTripsNotificationChannels()
        {
            var original = new PostExecution { Email = true, Psa = false, Webhook = true };

            var json = await KiotaTestHelpers.SerializeToJsonAsync(original);
            var roundTripped = await KiotaTestHelpers.DeserializeFromJsonAsync(json, PostExecution.CreateFromDiscriminatorValue);

            Assert.NotNull(roundTripped);
            Assert.True(roundTripped!.Email);
            Assert.False(roundTripped.Psa);
            Assert.True(roundTripped.Webhook);
        }

        [Fact]
        public async Task GroupRefRoundTripsValueAndLabel()
        {
            var original = new GroupRef
            {
                Label = "Engineering",
                Value = "00000000-0000-0000-0000-000000000001",
                AddedFields = new GroupRef_addedFields { GroupType = GroupRef_addedFields_groupType.Microsoft365 },
            };

            var json = await KiotaTestHelpers.SerializeToJsonAsync(original);
            var roundTripped = await KiotaTestHelpers.DeserializeFromJsonAsync(json, GroupRef.CreateFromDiscriminatorValue);

            Assert.NotNull(roundTripped);
            Assert.Equal("Engineering", roundTripped!.Label);
            Assert.Equal("00000000-0000-0000-0000-000000000001", roundTripped.Value);
        }

        [Fact]
        public async Task GroupRefRoundTripsNestedGroupType()
        {
            var original = new GroupRef
            {
                Value = "00000000-0000-0000-0000-000000000001",
                AddedFields = new GroupRef_addedFields { GroupType = GroupRef_addedFields_groupType.Microsoft365 },
            };

            var json = await KiotaTestHelpers.SerializeToJsonAsync(original);
            var roundTripped = await KiotaTestHelpers.DeserializeFromJsonAsync(json, GroupRef.CreateFromDiscriminatorValue);

            Assert.NotNull(roundTripped);
            Assert.NotNull(roundTripped!.AddedFields);
            Assert.Equal(GroupRef_addedFields_groupType.Microsoft365, roundTripped.AddedFields!.GroupType);
        }

        [Fact]
        public async Task GroupRefSerializesGroupTypeUsingEnumMemberValue()
        {
            var original = new GroupRef
            {
                AddedFields = new GroupRef_addedFields { GroupType = GroupRef_addedFields_groupType.Microsoft365 },
            };

            var json = await KiotaTestHelpers.SerializeToJsonAsync(original);

            Assert.Contains("\"groupType\":\"Microsoft 365\"", json);
        }
    }
}
