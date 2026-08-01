using Rock.Plugin;

namespace com.yesuchum.RockDetective.Migrations
{
    [MigrationNumber( 1, "18.2" )]
    public class _001_CreateSchema : Migration
    {
        private const string InstalledPluginsPage = "5B6DBC42-8B03-4D15-8D92-AAFA28FD8616";
        private const string InternalFullWidthLayout = "D65F783D-87A9-4CC9-8110-E83466A0EADB";
        private const string RockAdministrationGroup = "628C51A8-4613-43ED-A18D-4A6FB999273E";
        private const string IntegerFieldType = "A75DFC58-7A1B-4799-BF31-451B2BBE38FF";

        private const string CaseFileListBlock = "9680C14E-9D7E-4A18-95E0-43A75D1A3259";
        private const string CaseBoardBlock = "BE158897-103D-4F32-A2CC-F8A0D8EFA440";
        private const string WitnessStatementBlock = "FAEC6526-A689-4002-BB52-76C3DFF43C15";

        private const string AuthRoleView = "46C82060-A520-433A-BF82-0A3CABE9AD10";
        private const string AuthRoleEdit = "03C5F494-C142-4188-8DA7-1AA0915C039A";
        private const string AuthAdminView = "F26ABF28-ED66-4722-8C9F-FBDAB7B73C0C";
        private const string AuthAdminEdit = "69304709-C4C5-49A5-AB3C-654D1330D997";
        private const string AuthAdminAdministrate = "330E42B2-BABD-43D6-AD14-724A14F055FA";

        private const string ReminderDaysAttribute = "7CAC3FF9-A092-43FE-B6C5-8CDE388AC461";
        private const string DetailPageAttribute = "4AF30017-5374-4D14-8AAC-769B1649C6DC";
        private const string WitnessStatementPageAttribute = "F28BD323-EA0B-492E-98F9-5B7F4D5B902B";
        private const string PageReferenceFieldType = "BD53F9C9-EBA9-4D3F-82EA-DE5DD34A8108";

        private const string CaseFileEntityName = "com.yesuchum.RockDetective.Model.CaseFile";

        public override void Up()
        {
            CreateCaseFileTable();
            CreateCaseParticipantTable();
            CreateCaseStatementTable();
            CreateCaseEntryTable();
            CreateHypothesisTable();
            CreateExperimentTable();

            RegisterEntityTypes();
            SeedCategories();
            SeedSecurity();

            RegisterBlockTypes();
            CreatePages();
            AddSystemCommunications();
            AddJobs();
        }

        public override void Down()
        {
            DeleteJobs();
            DeleteSystemCommunications();
            DeletePages();
            UnregisterBlockTypes();
            DeleteSecurity();
            DeleteCategories();
            UnregisterEntityTypes();

            DropTable( "_com_yesuchum_RockDetective_CaseStatement" );
            DropTable( "_com_yesuchum_RockDetective_Experiment" );
            DropTable( "_com_yesuchum_RockDetective_Hypothesis" );
            DropTable( "_com_yesuchum_RockDetective_CaseEntry" );
            DropTable( "_com_yesuchum_RockDetective_CaseParticipant" );
            DropTable( "_com_yesuchum_RockDetective_CaseFile" );
        }

        private void CreateCaseFileTable()
        {
            const string table = "_com_yesuchum_RockDetective_CaseFile";

            AddTable( table, t => new
            {
                Id = t.Int( identity: true, nullable: false ),
                Title = t.String( nullable: false, maxLength: 200 ),
                ProblemStatement = t.String(),
                CategoryId = t.Int(),
                Mode = t.Int( nullable: false, defaultValue: 0 ),
                Classification = t.Int( nullable: false, defaultValue: 0 ),
                Status = t.Int( nullable: false, defaultValue: 0 ),
                CurrentPhase = t.Int( nullable: false, defaultValue: 2 ),
                PhaseStateJson = t.String(),
                OwnerPersonAliasId = t.Int( nullable: false ),
                ImpactSummary = t.String(),
                SolutionSummary = t.String(),
                ContributingFactors = t.String(),
                LessonsLearned = t.String(),
                PreventionMeasures = t.String(),
                ActionItems = t.String(),
                FollowUpDate = t.DateTime(),
                FollowUpConfirmedDateTime = t.DateTime(),
                ClosedDateTime = t.DateTime(),
                CreatedDateTime = t.DateTime(),
                ModifiedDateTime = t.DateTime(),
                CreatedByPersonAliasId = t.Int(),
                ModifiedByPersonAliasId = t.Int(),
                Guid = t.Guid( nullable: false, defaultValueSql: "newid()" ),
                ForeignId = t.Int(),
                ForeignGuid = t.Guid(),
                ForeignKey = t.String( maxLength: 100 ),
            } );

            AddPrimaryKey( table, "Id" );
            AddIndex( table, "Guid", unique: true );
            AddIndex( table, new[] { "Status", "CategoryId" }, name: "IX_StatusCategory" );
            AddIndex( table, "OwnerPersonAliasId" );
        }

        private void CreateCaseParticipantTable()
        {
            const string table = "_com_yesuchum_RockDetective_CaseParticipant";

            AddTable( table, t => new
            {
                Id = t.Int( identity: true, nullable: false ),
                CaseFileId = t.Int( nullable: false ),
                PersonAliasId = t.Int(),
                ExternalName = t.String( maxLength: 200 ),
                ExternalContact = t.String( maxLength: 400 ),
                Role = t.Int( nullable: false, defaultValue: 4 ),
                CreatedDateTime = t.DateTime(),
                ModifiedDateTime = t.DateTime(),
                CreatedByPersonAliasId = t.Int(),
                ModifiedByPersonAliasId = t.Int(),
                Guid = t.Guid( nullable: false, defaultValueSql: "newid()" ),
                ForeignId = t.Int(),
                ForeignGuid = t.Guid(),
                ForeignKey = t.String( maxLength: 100 ),
            } );

            AddPrimaryKey( table, "Id" );
            AddIndex( table, "Guid", unique: true );
            AddIndex( table, "CaseFileId" );
            AddIndex( table, "PersonAliasId" );

            Sql( $@"
                ALTER TABLE [{table}] ADD CONSTRAINT [FK_{table}_CaseFile]
                FOREIGN KEY ([CaseFileId]) REFERENCES [_com_yesuchum_RockDetective_CaseFile]([Id])
            " );
        }

        private void CreateCaseStatementTable()
        {
            const string table = "_com_yesuchum_RockDetective_CaseStatement";

            AddTable( table, t => new
            {
                Id = t.Int( identity: true, nullable: false ),
                CaseFileId = t.Int( nullable: false ),
                ParticipantId = t.Int( nullable: false ),
                RequestPrompt = t.String(),
                RequestStatus = t.Int( nullable: false, defaultValue: 0 ),
                RequestDateTime = t.DateTime(),
                SentDateTime = t.DateTime(),
                SubmittedDateTime = t.DateTime(),
                Statement = t.String(),
                BinaryFileId = t.Int(),
                Url = t.String( maxLength: 2000 ),
                CreatedDateTime = t.DateTime(),
                ModifiedDateTime = t.DateTime(),
                CreatedByPersonAliasId = t.Int(),
                ModifiedByPersonAliasId = t.Int(),
                Guid = t.Guid( nullable: false, defaultValueSql: "newid()" ),
                ForeignId = t.Int(),
                ForeignGuid = t.Guid(),
                ForeignKey = t.String( maxLength: 100 ),
            } );

            AddPrimaryKey( table, "Id" );
            AddIndex( table, "Guid", unique: true );
            AddIndex( table, "CaseFileId" );
            AddIndex( table, "ParticipantId" );

            Sql( $@"
                ALTER TABLE [{table}] ADD CONSTRAINT [FK_{table}_CaseFile]
                FOREIGN KEY ([CaseFileId]) REFERENCES [_com_yesuchum_RockDetective_CaseFile]([Id]);

                ALTER TABLE [{table}] ADD CONSTRAINT [FK_{table}_Participant]
                FOREIGN KEY ([ParticipantId]) REFERENCES [_com_yesuchum_RockDetective_CaseParticipant]([Id]);
            " );
        }

        private void CreateCaseEntryTable()
        {
            const string table = "_com_yesuchum_RockDetective_CaseEntry";

            AddTable( table, t => new
            {
                Id = t.Int( identity: true, nullable: false ),
                CaseFileId = t.Int( nullable: false ),
                EntryType = t.Int( nullable: false, defaultValue: 0 ),
                Text = t.String( nullable: false ),
                EventDateTime = t.DateTime(),
                TimelineEventType = t.Int(),
                BinaryFileId = t.Int(),
                Url = t.String( maxLength: 2000 ),
                Source = t.Int( nullable: false, defaultValue: 0 ),
                SourceParticipantId = t.Int(),
                SourceExperimentId = t.Int(),
                IsCorroborated = t.Boolean( nullable: false, defaultValue: false ),
                Order = t.Int( nullable: false, defaultValue: 0 ),
                CreatedDateTime = t.DateTime(),
                ModifiedDateTime = t.DateTime(),
                CreatedByPersonAliasId = t.Int(),
                ModifiedByPersonAliasId = t.Int(),
                Guid = t.Guid( nullable: false, defaultValueSql: "newid()" ),
                ForeignId = t.Int(),
                ForeignGuid = t.Guid(),
                ForeignKey = t.String( maxLength: 100 ),
            } );

            AddPrimaryKey( table, "Id" );
            AddIndex( table, "Guid", unique: true );
            AddIndex( table, new[] { "CaseFileId", "EntryType" }, name: "IX_CaseEntryType" );

            Sql( $@"
                ALTER TABLE [{table}] ADD CONSTRAINT [FK_{table}_CaseFile]
                FOREIGN KEY ([CaseFileId]) REFERENCES [_com_yesuchum_RockDetective_CaseFile]([Id])
            " );
        }

        private void CreateHypothesisTable()
        {
            const string table = "_com_yesuchum_RockDetective_Hypothesis";

            AddTable( table, t => new
            {
                Id = t.Int( identity: true, nullable: false ),
                CaseFileId = t.Int( nullable: false ),
                ParentHypothesisId = t.Int(),
                Name = t.String( nullable: false, maxLength: 300 ),
                Description = t.String(),
                Probability = t.Int( nullable: false, defaultValue: 0 ),
                Status = t.Int( nullable: false, defaultValue: 0 ),
                RuledOutReason = t.String(),
                CreatedDateTime = t.DateTime(),
                ModifiedDateTime = t.DateTime(),
                CreatedByPersonAliasId = t.Int(),
                ModifiedByPersonAliasId = t.Int(),
                Guid = t.Guid( nullable: false, defaultValueSql: "newid()" ),
                ForeignId = t.Int(),
                ForeignGuid = t.Guid(),
                ForeignKey = t.String( maxLength: 100 ),
            } );

            AddPrimaryKey( table, "Id" );
            AddIndex( table, "Guid", unique: true );
            AddIndex( table, "CaseFileId" );
            AddIndex( table, "ParentHypothesisId" );

            Sql( $@"
                ALTER TABLE [{table}] ADD CONSTRAINT [FK_{table}_CaseFile]
                FOREIGN KEY ([CaseFileId]) REFERENCES [_com_yesuchum_RockDetective_CaseFile]([Id]);

                ALTER TABLE [{table}] ADD CONSTRAINT [FK_{table}_Parent]
                FOREIGN KEY ([ParentHypothesisId]) REFERENCES [{table}]([Id]);
            " );
        }

        private void CreateExperimentTable()
        {
            const string table = "_com_yesuchum_RockDetective_Experiment";

            AddTable( table, t => new
            {
                Id = t.Int( identity: true, nullable: false ),
                CaseFileId = t.Int( nullable: false ),
                HypothesisId = t.Int(),
                HypothesisText = t.String( maxLength: 300 ),
                TestPlan = t.String(),
                ExpectedResult = t.String(),
                ActualResult = t.String(),
                Outcome = t.Int( nullable: false, defaultValue: 0 ),
                ChangesMade = t.String(),
                ChangesReverted = t.Boolean( nullable: false, defaultValue: false ),
                RevertJustification = t.String(),
                Learnings = t.String(),
                CreatedDateTime = t.DateTime(),
                ModifiedDateTime = t.DateTime(),
                CreatedByPersonAliasId = t.Int(),
                ModifiedByPersonAliasId = t.Int(),
                Guid = t.Guid( nullable: false, defaultValueSql: "newid()" ),
                ForeignId = t.Int(),
                ForeignGuid = t.Guid(),
                ForeignKey = t.String( maxLength: 100 ),
            } );

            AddPrimaryKey( table, "Id" );
            AddIndex( table, "Guid", unique: true );
            AddIndex( table, "CaseFileId" );
            AddIndex( table, "HypothesisId" );

            Sql( $@"
                ALTER TABLE [{table}] ADD CONSTRAINT [FK_{table}_CaseFile]
                FOREIGN KEY ([CaseFileId]) REFERENCES [_com_yesuchum_RockDetective_CaseFile]([Id]);

                ALTER TABLE [{table}] ADD CONSTRAINT [FK_{table}_Hypothesis]
                FOREIGN KEY ([HypothesisId]) REFERENCES [_com_yesuchum_RockDetective_Hypothesis]([Id]);
            " );
        }

        private void RegisterEntityTypes()
        {
            RockMigrationHelper.UpdateEntityType( CaseFileEntityName, "Case File", "com.yesuchum.RockDetective", true, true, SystemGuid.EntityType.CASE_FILE );
            RockMigrationHelper.UpdateEntityType( "com.yesuchum.RockDetective.Model.CaseParticipant", "Case Participant", "com.yesuchum.RockDetective", true, false, SystemGuid.EntityType.CASE_PARTICIPANT );
            RockMigrationHelper.UpdateEntityType( "com.yesuchum.RockDetective.Model.CaseEntry", "Case Entry", "com.yesuchum.RockDetective", true, false, SystemGuid.EntityType.CASE_ENTRY );
            RockMigrationHelper.UpdateEntityType( "com.yesuchum.RockDetective.Model.Hypothesis", "Hypothesis", "com.yesuchum.RockDetective", true, false, SystemGuid.EntityType.HYPOTHESIS );
            RockMigrationHelper.UpdateEntityType( "com.yesuchum.RockDetective.Model.Experiment", "Experiment", "com.yesuchum.RockDetective", true, false, SystemGuid.EntityType.EXPERIMENT );
            RockMigrationHelper.UpdateEntityType( "com.yesuchum.RockDetective.Model.CaseStatement", "Case Statement", "com.yesuchum.RockDetective", true, false, SystemGuid.EntityType.CASE_STATEMENT );

            RockMigrationHelper.UpdateEntityType( "com.yesuchum.RockDetective.Blocks.CaseFileList", "Case File List", "com.yesuchum.RockDetective", false, false, SystemGuid.EntityType.CASE_FILE_LIST_BLOCK );
            RockMigrationHelper.UpdateEntityType( "com.yesuchum.RockDetective.Blocks.CaseFileDetail", "Case Board", "com.yesuchum.RockDetective", false, false, SystemGuid.EntityType.CASE_BOARD_BLOCK );
            RockMigrationHelper.UpdateEntityType( "com.yesuchum.RockDetective.Blocks.WitnessStatementEntry", "Witness Statement Entry", "com.yesuchum.RockDetective", false, false, SystemGuid.EntityType.WITNESS_STATEMENT_ENTRY_BLOCK );
        }

        private void UnregisterEntityTypes()
        {
            RockMigrationHelper.DeleteEntityType( SystemGuid.EntityType.CASE_STATEMENT );
            RockMigrationHelper.DeleteEntityType( SystemGuid.EntityType.EXPERIMENT );
            RockMigrationHelper.DeleteEntityType( SystemGuid.EntityType.HYPOTHESIS );
            RockMigrationHelper.DeleteEntityType( SystemGuid.EntityType.CASE_ENTRY );
            RockMigrationHelper.DeleteEntityType( SystemGuid.EntityType.CASE_PARTICIPANT );
            RockMigrationHelper.DeleteEntityType( SystemGuid.EntityType.CASE_FILE );
        }

        private void SeedCategories()
        {
            RockMigrationHelper.UpdateCategory( SystemGuid.EntityType.CASE_FILE, "Software Bug", "ti ti-bug", "Defects in software behavior.", SystemGuid.Category.SOFTWARE_BUG, 0 );
            RockMigrationHelper.UpdateCategory( SystemGuid.EntityType.CASE_FILE, "Performance", "ti ti-gauge", "Slowness, resource exhaustion, and capacity problems.", SystemGuid.Category.PERFORMANCE, 1 );
            RockMigrationHelper.UpdateCategory( SystemGuid.EntityType.CASE_FILE, "Process", "ti ti-route", "Broken or inefficient processes and procedures.", SystemGuid.Category.PROCESS, 2 );
            RockMigrationHelper.UpdateCategory( SystemGuid.EntityType.CASE_FILE, "Ministry Operations", "ti ti-building-church", "Ministry-level operational problems such as attendance or engagement patterns.", SystemGuid.Category.MINISTRY_OPERATIONS, 3 );
        }

        private void DeleteCategories()
        {
            RockMigrationHelper.DeleteCategory( SystemGuid.Category.MINISTRY_OPERATIONS );
            RockMigrationHelper.DeleteCategory( SystemGuid.Category.PROCESS );
            RockMigrationHelper.DeleteCategory( SystemGuid.Category.PERFORMANCE );
            RockMigrationHelper.DeleteCategory( SystemGuid.Category.SOFTWARE_BUG );
        }

        private void SeedSecurity()
        {
            Sql( $@"
                IF NOT EXISTS ( SELECT 1 FROM [Group] WHERE [Guid] = '{SystemGuid.Group.ROCK_DETECTIVE_USERS}' )
                BEGIN
                    DECLARE @SecurityRoleGroupTypeId int = ( SELECT TOP 1 [Id] FROM [GroupType] WHERE [Guid] = 'AECE949F-704C-483E-A4FB-93D5E4720C4C' );

                    INSERT INTO [Group] ([IsSystem], [GroupTypeId], [Name], [Description], [IsSecurityRole], [IsActive], [IsPublic], [Order], [Guid])
                    VALUES (0, @SecurityRoleGroupTypeId, 'Rock Detective Users', 'Members can view and edit Rock Detective case files.', 1, 1, 1, 0, '{SystemGuid.Group.ROCK_DETECTIVE_USERS}')
                END
            " );

            RockMigrationHelper.AddSecurityAuthForEntityType( CaseFileEntityName, 0, Rock.Security.Authorization.VIEW, true, SystemGuid.Group.ROCK_DETECTIVE_USERS, 0, AuthRoleView );
            RockMigrationHelper.AddSecurityAuthForEntityType( CaseFileEntityName, 1, Rock.Security.Authorization.EDIT, true, SystemGuid.Group.ROCK_DETECTIVE_USERS, 0, AuthRoleEdit );
            RockMigrationHelper.AddSecurityAuthForEntityType( CaseFileEntityName, 2, Rock.Security.Authorization.VIEW, true, RockAdministrationGroup, 0, AuthAdminView );
            RockMigrationHelper.AddSecurityAuthForEntityType( CaseFileEntityName, 3, Rock.Security.Authorization.EDIT, true, RockAdministrationGroup, 0, AuthAdminEdit );
            RockMigrationHelper.AddSecurityAuthForEntityType( CaseFileEntityName, 4, Rock.Security.Authorization.ADMINISTRATE, true, RockAdministrationGroup, 0, AuthAdminAdministrate );
        }

        private void DeleteSecurity()
        {
            RockMigrationHelper.DeleteSecurityAuth( AuthAdminAdministrate );
            RockMigrationHelper.DeleteSecurityAuth( AuthAdminEdit );
            RockMigrationHelper.DeleteSecurityAuth( AuthAdminView );
            RockMigrationHelper.DeleteSecurityAuth( AuthRoleEdit );
            RockMigrationHelper.DeleteSecurityAuth( AuthRoleView );
            RockMigrationHelper.DeleteSecurityRoleGroup( SystemGuid.Group.ROCK_DETECTIVE_USERS );
        }

        private void RegisterBlockTypes()
        {
            RockMigrationHelper.AddOrUpdateEntityBlockType(
                "Case File List",
                "Lists Rock Detective case files with My Requests, My Assignments, and All views, plus the closed-case archive search.",
                "com.yesuchum.RockDetective.Blocks.CaseFileList",
                "Rock Detective",
                SystemGuid.BlockType.CASE_FILE_LIST );

            RockMigrationHelper.AddOrUpdateEntityBlockType(
                "Case Board",
                "The Rock Detective case workspace: phase stepper, phase forms with gate checklists, facts, probability tree, experiments, timeline, evidence, witnesses, and team.",
                "com.yesuchum.RockDetective.Blocks.CaseFileDetail",
                "Rock Detective",
                SystemGuid.BlockType.CASE_FILE_DETAIL );

            RockMigrationHelper.AddOrUpdateEntityBlockType(
                "Witness Statement Entry",
                "Landing page for witness statement requests; the requested witness can submit or decline their statement.",
                "com.yesuchum.RockDetective.Blocks.WitnessStatementEntry",
                "Rock Detective",
                SystemGuid.BlockType.WITNESS_STATEMENT_ENTRY );
        }

        private void UnregisterBlockTypes()
        {
            RockMigrationHelper.DeleteBlockType( SystemGuid.BlockType.WITNESS_STATEMENT_ENTRY );
            RockMigrationHelper.DeleteBlockType( SystemGuid.BlockType.CASE_FILE_DETAIL );
            RockMigrationHelper.DeleteBlockType( SystemGuid.BlockType.CASE_FILE_LIST );
        }

        private void CreatePages()
        {
            RockMigrationHelper.AddPage( true, InstalledPluginsPage, InternalFullWidthLayout,
                "Rock Detective",
                "Case-file-driven problem solving using the Triumphant Problem Solving method.",
                SystemGuid.Page.ROCK_DETECTIVE, "ti ti-zoom-question" );

            RockMigrationHelper.AddPage( true, SystemGuid.Page.ROCK_DETECTIVE, InternalFullWidthLayout,
                "Case Board",
                "Workspace for a single case file.",
                SystemGuid.Page.CASE_BOARD, "ti ti-clipboard-list" );

            RockMigrationHelper.AddPage( true, SystemGuid.Page.ROCK_DETECTIVE, InternalFullWidthLayout,
                "Witness Statement",
                "Submit a requested witness statement for a case.",
                SystemGuid.Page.WITNESS_STATEMENT, "ti ti-message-report" );

            Sql( $@"
                UPDATE [Page] SET [DisplayInNavWhen] = 2
                WHERE [Guid] IN ('{SystemGuid.Page.CASE_BOARD}', '{SystemGuid.Page.WITNESS_STATEMENT}')
            " );

            RockMigrationHelper.AddBlock( true, SystemGuid.Page.ROCK_DETECTIVE, null, SystemGuid.BlockType.CASE_FILE_LIST,
                "Case File List", "Main", string.Empty, string.Empty, 0, CaseFileListBlock );

            RockMigrationHelper.AddBlock( true, SystemGuid.Page.CASE_BOARD, null, SystemGuid.BlockType.CASE_FILE_DETAIL,
                "Case Board", "Main", string.Empty, string.Empty, 0, CaseBoardBlock );

            RockMigrationHelper.AddBlock( true, SystemGuid.Page.WITNESS_STATEMENT, null, SystemGuid.BlockType.WITNESS_STATEMENT_ENTRY,
                "Witness Statement Entry", "Main", string.Empty, string.Empty, 0, WitnessStatementBlock );

            RockMigrationHelper.AddOrUpdateBlockTypeAttribute( SystemGuid.BlockType.CASE_FILE_LIST, PageReferenceFieldType,
                "Detail Page", "DetailPage", "Detail Page",
                "The Case Board page for viewing and working a case.",
                0, string.Empty, DetailPageAttribute );

            RockMigrationHelper.AddOrUpdateBlockTypeAttribute( SystemGuid.BlockType.CASE_FILE_DETAIL, PageReferenceFieldType,
                "Witness Statement Page", "WitnessStatementPage", "Witness Statement Page",
                "The page witnesses land on to submit a requested statement.",
                0, string.Empty, WitnessStatementPageAttribute );

            RockMigrationHelper.AddBlockAttributeValue( true, CaseFileListBlock, DetailPageAttribute, SystemGuid.Page.CASE_BOARD );
            RockMigrationHelper.AddBlockAttributeValue( true, CaseBoardBlock, WitnessStatementPageAttribute, SystemGuid.Page.WITNESS_STATEMENT );
        }

        private void DeletePages()
        {
            RockMigrationHelper.DeleteAttribute( WitnessStatementPageAttribute );
            RockMigrationHelper.DeleteAttribute( DetailPageAttribute );
            RockMigrationHelper.DeleteBlock( WitnessStatementBlock );
            RockMigrationHelper.DeleteBlock( CaseBoardBlock );
            RockMigrationHelper.DeleteBlock( CaseFileListBlock );
            RockMigrationHelper.DeletePage( SystemGuid.Page.WITNESS_STATEMENT );
            RockMigrationHelper.DeletePage( SystemGuid.Page.CASE_BOARD );
            RockMigrationHelper.DeletePage( SystemGuid.Page.ROCK_DETECTIVE );
        }

        private void AddSystemCommunications()
        {
            RockMigrationHelper.UpdateSystemCommunication(
                "Plugins",
                "Rock Detective Witness Statement Request",
                string.Empty, string.Empty, string.Empty, string.Empty, string.Empty,
                "Statement requested: {{ CaseTitle }}",
                @"{{ 'Global' | Attribute:'EmailHeader' }}
<p>Hi {{ Person.NickName }},</p>
<p>{{ RequestorName }} is investigating a problem and has asked you for a written statement.</p>
<p><strong>Case:</strong> {{ CaseNumber }}; {{ CaseTitle }}</p>
<p><strong>What they are asking:</strong></p>
<blockquote>{{ RequestPrompt }}</blockquote>
<p><a href=""{{ StatementLink }}"">Submit your statement</a></p>
<p>Specific, firsthand observations help most: what you saw, when you saw it, and what changed around that time.</p>
{{ 'Global' | Attribute:'EmailFooter' }}",
                SystemGuid.SystemCommunication.WITNESS_STATEMENT_REQUEST );

            RockMigrationHelper.UpdateSystemCommunication(
                "Plugins",
                "Rock Detective Witness Statement Reminder",
                string.Empty, string.Empty, string.Empty, string.Empty, string.Empty,
                "Reminder: statement requested for {{ CaseTitle }}",
                @"{{ 'Global' | Attribute:'EmailHeader' }}
<p>Hi {{ Person.NickName }},</p>
<p>This is a reminder that your statement is still needed for case {{ CaseNumber }}; {{ CaseTitle }}.</p>
<p><strong>What they are asking:</strong></p>
<blockquote>{{ RequestPrompt }}</blockquote>
<p><a href=""{{ StatementLink }}"">Submit your statement</a></p>
{{ 'Global' | Attribute:'EmailFooter' }}",
                SystemGuid.SystemCommunication.WITNESS_STATEMENT_REMINDER );

            RockMigrationHelper.UpdateSystemCommunication(
                "Plugins",
                "Rock Detective Follow-Up Check",
                string.Empty, string.Empty, string.Empty, string.Empty, string.Empty,
                "Follow-up check: {{ CaseTitle }}",
                @"{{ 'Global' | Attribute:'EmailHeader' }}
<p>Hi {{ Person.NickName }},</p>
<p>Case {{ CaseNumber }}; {{ CaseTitle }} was closed with a follow-up check scheduled for today.</p>
<p>Has the fix held?</p>
<p><a href=""{{ FixHeldLink }}"">Yes, the fix held</a></p>
<p><a href=""{{ ReopenLink }}"">No, reopen the case</a></p>
{{ 'Global' | Attribute:'EmailFooter' }}",
                SystemGuid.SystemCommunication.FOLLOW_UP_CHECK );
        }

        private void DeleteSystemCommunications()
        {
            RockMigrationHelper.DeleteSystemCommunication( SystemGuid.SystemCommunication.FOLLOW_UP_CHECK );
            RockMigrationHelper.DeleteSystemCommunication( SystemGuid.SystemCommunication.WITNESS_STATEMENT_REMINDER );
            RockMigrationHelper.DeleteSystemCommunication( SystemGuid.SystemCommunication.WITNESS_STATEMENT_REQUEST );
        }

        private void AddJobs()
        {
            Sql( $@"
                IF NOT EXISTS (SELECT 1 FROM [ServiceJob] WHERE [Guid] = '{SystemGuid.EntityType.WITNESS_REMINDER_JOB}')
                BEGIN
                    INSERT INTO [ServiceJob] ([IsSystem], [IsActive], [Name], [Description], [Class], [CronExpression], [NotificationStatus], [Guid])
                    VALUES (0, 1,
                        'Rock Detective Witness Reminders',
                        'Sends reminder communications for witness statement requests that have gone unanswered longer than the configured number of days.',
                        'com.yesuchum.RockDetective.Jobs.SendWitnessReminders',
                        '0 0 7 1/1 * ? *', 3,
                        '{SystemGuid.EntityType.WITNESS_REMINDER_JOB}')
                END;

                IF NOT EXISTS (SELECT 1 FROM [ServiceJob] WHERE [Guid] = '{SystemGuid.EntityType.FOLLOW_UP_CHECK_JOB}')
                BEGIN
                    INSERT INTO [ServiceJob] ([IsSystem], [IsActive], [Name], [Description], [Class], [CronExpression], [NotificationStatus], [Guid])
                    VALUES (0, 1,
                        'Rock Detective Follow-Up Checks',
                        'Notifies case owners when a closed case reaches its follow-up check date so they can confirm the fix held or reopen the case.',
                        'com.yesuchum.RockDetective.Jobs.ProcessFollowUpChecks',
                        '0 15 7 1/1 * ? *', 3,
                        '{SystemGuid.EntityType.FOLLOW_UP_CHECK_JOB}')
                END;
            " );

            RockMigrationHelper.AddOrUpdateEntityAttribute(
                "Rock.Model.ServiceJob", IntegerFieldType,
                "Class", "com.yesuchum.RockDetective.Jobs.SendWitnessReminders",
                "Reminder Days", "Reminder Days",
                "Number of days a witness statement request may sit unanswered before a reminder is sent.",
                0, "3", ReminderDaysAttribute, "ReminderDays" );
        }

        private void DeleteJobs()
        {
            RockMigrationHelper.DeleteAttribute( ReminderDaysAttribute );

            Sql( $@"
                DELETE FROM [ServiceJob] WHERE [Guid] IN (
                    '{SystemGuid.EntityType.WITNESS_REMINDER_JOB}',
                    '{SystemGuid.EntityType.FOLLOW_UP_CHECK_JOB}')
            " );
        }
    }
}
