using System;
using System.Threading.Tasks;
using Marten.Testing.Harness;
using Shouldly;
using Xunit;

namespace Marten.Testing.Acceptance
{

    public class MetadataTarget
    {
        public Guid Id { get; set; }
        public string Name { get; set; }

        public string CausationId { get; set; }
        public string CorrelationId { get; set; }
        public string LastModifiedBy { get; set; }
    }

    [Collection("metadata")]
    public class when_mapping_to_the_correlation_tracking : FlexibleDocumentMetadataContext
    {
        public when_mapping_to_the_correlation_tracking() : base("metadata")
        {
        }

        protected override void MetadataIs(MartenRegistry.DocumentMappingExpression<MetadataTarget>.MetadataConfig metadata)
        {
            metadata.CorrelationId.MapTo(x => x.CorrelationId);
            metadata.CausationId.MapTo(x => x.CausationId);
            metadata.LastModifiedBy.MapTo(x => x.LastModifiedBy);
        }

        [Fact]
        public async Task save_and_load_metadata_causation()
        {
            var doc = new MetadataTarget();

            theSession.Store(doc);
            await theSession.SaveChangesAsync();

            var metadata = await theSession.MetadataForAsync(doc);

            metadata.CausationId.ShouldBe(theSession.CausationId);
            //metadata.CorrelationId.ShouldBe(theSession.CorrelationId);
            //metadata.LastModifiedBy.ShouldBe(theSession.LastModifiedBy);

            using (var session2 = theStore.QuerySession())
            {
                var doc2 = await session2.LoadAsync<MetadataTarget>(doc.Id);
                doc2.CausationId.ShouldBe(theSession.CausationId);
                //doc2.CorrelationId.ShouldBe(theSession.CorrelationId);
                //doc2.LastModifiedBy.ShouldBe(theSession.LastModifiedBy);
            }

        }

        [Fact]
        public async Task save_and_load_metadata_correlation()
        {
            var doc = new MetadataTarget();

            theSession.Store(doc);
            await theSession.SaveChangesAsync();

            var metadata = await theSession.MetadataForAsync(doc);

            metadata.CorrelationId.ShouldBe(theSession.CorrelationId);

            using (var session2 = theStore.QuerySession())
            {
                var doc2 = await session2.LoadAsync<MetadataTarget>(doc.Id);
                doc2.CorrelationId.ShouldBe(theSession.CorrelationId);
            }

        }

        [Fact]
        public async Task save_and_load_metadata_last_modified_by()
        {
            var doc = new MetadataTarget();

            theSession.Store(doc);
            await theSession.SaveChangesAsync();

            var metadata = await theSession.MetadataForAsync(doc);

            metadata.LastModifiedBy.ShouldBe(theSession.LastModifiedBy);

            using (var session2 = theStore.QuerySession())
            {
                var doc2 = await session2.LoadAsync<MetadataTarget>(doc.Id);
                doc2.LastModifiedBy.ShouldBe(theSession.LastModifiedBy);
            }

        }
    }

    [Collection("metadata")]
    public class when_turning_off_all_optional_metadata: FlexibleDocumentMetadataContext
    {
        public when_turning_off_all_optional_metadata() : base("metadata")
        {
        }

        protected override void MetadataIs(MartenRegistry.DocumentMappingExpression<MetadataTarget>.MetadataConfig metadata)
        {
            metadata.DisableInformationalFields();
        }
    }

    public abstract class FlexibleDocumentMetadataContext : OneOffConfigurationsContext
    {
        protected FlexibleDocumentMetadataContext(string schemaName) : base(schemaName)
        {
            StoreOptions(opts =>
            {
                opts.Schema.For<MetadataTarget>()
                    .Metadata(MetadataIs);
            });

            theSession.CorrelationId = "The Correlation";
            theSession.CausationId = "The Cause";
            theSession.LastModifiedBy = "Last Person";
        }

        protected abstract void MetadataIs(MartenRegistry.DocumentMappingExpression<MetadataTarget>.MetadataConfig metadata);

        [Fact]
        public void can_bulk_insert()
        {
            var docs = new MetadataTarget[]
            {
                new MetadataTarget(),
                new MetadataTarget(),
                new MetadataTarget(),
                new MetadataTarget(),
                new MetadataTarget(),
                new MetadataTarget()
            };

            theStore.BulkInsert(docs);
        }

        [Fact]
        public async Task can_save_and_load()
        {
            var doc = new MetadataTarget();
            theSession.Store(doc);
            await theSession.SaveChangesAsync();

            using var session = theStore.LightweightSession();
            var doc2 = session.LoadAsync<MetadataTarget>(doc.Id);
            SpecificationExtensions.ShouldNotBeNull(doc2);
        }

        [Fact]
        public async Task can_insert_and_load()
        {
            var doc = new MetadataTarget();
            theSession.Insert(doc);
            await theSession.SaveChangesAsync();

            using var session = theStore.LightweightSession();
            var doc2 = session.LoadAsync<MetadataTarget>(doc.Id);
            doc2.ShouldNotBeNull();
        }

        [Fact]
        public async Task can_save_update_and_load_lightweight()
        {
            var doc = new MetadataTarget();
            theSession.Store(doc);
            await theSession.SaveChangesAsync();

            doc.Name = "different";
            theSession.Update(doc);
            await theSession.SaveChangesAsync();

            using var session = theStore.LightweightSession();
            var doc2 = await session.LoadAsync<MetadataTarget>(doc.Id);
            doc2.Name.ShouldBe("different");
        }

        [Fact]
        public async Task can_save_update_and_load_queryonly()
        {
            var doc = new MetadataTarget();
            theSession.Store(doc);
            await theSession.SaveChangesAsync();

            doc.Name = "different";
            theSession.Update(doc);
            await theSession.SaveChangesAsync();

            using var session = theStore.QuerySession();
            var doc2 = await session.LoadAsync<MetadataTarget>(doc.Id);
            doc2.Name.ShouldBe("different");
        }

        [Fact]
        public async Task can_save_update_and_load_with_identity_map()
        {
            using var session = theStore.OpenSession();

            var doc = new MetadataTarget();
            session.Store(doc);
            await session.SaveChangesAsync();

            doc.Name = "different";
            session.Update(doc);
            await session.SaveChangesAsync();

            using var session2 = theStore.OpenSession();
            var doc2 = await session2.LoadAsync<MetadataTarget>(doc.Id);
            doc2.Name.ShouldBe("different");
        }

        [Fact]
        public async Task can_save_update_and_load_with_dirty_session()
        {
            var doc = new MetadataTarget();
            using var session = theStore.DirtyTrackedSession();
            session.Store(doc);
            await session.SaveChangesAsync();

            doc.Name = "different";
            theSession.Update(doc);
            await theSession.SaveChangesAsync();

            using var session2 = theStore.DirtyTrackedSession();
            var doc2 = await session2.LoadAsync<MetadataTarget>(doc.Id);
            doc2.Name.ShouldBe("different");
        }
    }
}
