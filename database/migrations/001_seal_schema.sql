-- ============================================================================
-- [DB-SCHEMA] SEAL Hackathon Platform — full Postgres schema (structure only)
-- Nguon: pg_dump --schema-only tu DB dev thuc te (Postgres 18), da doi chieu
-- dung voi entity C# hien tai (EF Code-First, KHONG dung EF Migrations trong
-- repo backend — DB duoc quan ly bang tay/SQL nhu file nay).
-- 20 bang: Users, Schools, Events, Rounds, Tracks, Teams, TeamInvitations,
-- EventRoles, EventRoleInvitations, SubmitResults, Scores, ScoreDetails,
-- Templates, Criterias, TemplateCriterias, FinalResults, Prizes, Appeals,
-- UserRejections, __EFMigrationsHistory.
-- Cach dung: tao DB rong roi chay file nay bang psql truoc, sau do chay
-- database/seed/001_seed_roles.sql de co du tai khoan test cho tung role.
-- ============================================================================
--
-- PostgreSQL database dump
--


-- Dumped from database version 18.4
-- Dumped by pg_dump version 18.4

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET transaction_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

--


--
-- Name: pgcrypto; Type: EXTENSION; Schema: -; Owner: -
--

CREATE EXTENSION IF NOT EXISTS pgcrypto WITH SCHEMA public;


--
-- Name: EXTENSION pgcrypto; Type: COMMENT; Schema: -; Owner: -
--

COMMENT ON EXTENSION pgcrypto IS 'cryptographic functions';


SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- Name: Appeals; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."Appeals" (
    "Id" text NOT NULL,
    "TeamId" text NOT NULL,
    "SubmitResultId" text NOT NULL,
    "Reason" text NOT NULL,
    "Response" text,
    "Status" text NOT NULL,
    "CreatedBy" text,
    "LastUpdatedBy" text,
    "DeletedBy" text,
    "CreatedTime" timestamp with time zone NOT NULL,
    "LastUpdatedTime" timestamp with time zone NOT NULL,
    "DeletedTime" timestamp with time zone,
    "AssignedJudgeId" text
);


--
-- Name: Criterias; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."Criterias" (
    "Id" text NOT NULL,
    "CriteriaName" character varying(255) NOT NULL,
    "Description" character varying(1000),
    "IsActive" boolean NOT NULL,
    "CreatedBy" text,
    "LastUpdatedBy" text,
    "DeletedBy" text,
    "CreatedTime" timestamp with time zone NOT NULL,
    "LastUpdatedTime" timestamp with time zone NOT NULL,
    "DeletedTime" timestamp with time zone
);


--
-- Name: EventRoleInvitations; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."EventRoleInvitations" (
    "Id" text NOT NULL,
    "EventId" text NOT NULL,
    "TrackId" text,
    "InvitedUserId" text,
    "InvitedByUserId" text,
    "RoleName" character varying(30) NOT NULL,
    "Status" character varying(30) NOT NULL,
    "ExpiresAt" timestamp with time zone NOT NULL,
    "RespondedAt" timestamp with time zone,
    "Notes" character varying(500),
    "CreatedBy" text,
    "LastUpdatedBy" text,
    "DeletedBy" text,
    "CreatedTime" timestamp with time zone NOT NULL,
    "LastUpdatedTime" timestamp with time zone NOT NULL,
    "DeletedTime" timestamp with time zone,
    "InvitedEmail" character varying(256),
    "InvitedFullName" character varying(256),
    "Token" character varying(100),
    "CreatedEventRoleId" text
);


--
-- Name: EventRoles; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."EventRoles" (
    "Id" text NOT NULL,
    "UserId" text NOT NULL,
    "EventId" text NOT NULL,
    "TeamId" text,
    "TrackId" text,
    "RoleName" character varying(50) NOT NULL,
    "AssignedAt" timestamp with time zone,
    "ExpiredAt" timestamp with time zone,
    "Notes" character varying(1000),
    "CreatedBy" text,
    "LastUpdatedBy" text,
    "DeletedBy" text,
    "CreatedTime" timestamp with time zone NOT NULL,
    "LastUpdatedTime" timestamp with time zone NOT NULL,
    "DeletedTime" timestamp with time zone,
    "Status" character varying(30) DEFAULT 'Active'::character varying NOT NULL
);


--
-- Name: Events; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."Events" (
    "Id" text NOT NULL,
    "EventName" character varying(255) NOT NULL,
    "Season" character varying(100),
    "Year" integer NOT NULL,
    "StartDate" timestamp with time zone NOT NULL,
    "EndDate" timestamp with time zone NOT NULL,
    "Description" character varying(1000),
    "CreatedBy" text,
    "LastUpdatedBy" text,
    "DeletedBy" text,
    "CreatedTime" timestamp with time zone NOT NULL,
    "LastUpdatedTime" timestamp with time zone NOT NULL,
    "DeletedTime" timestamp with time zone,
    "Status" boolean DEFAULT false NOT NULL,
    "PhotoEventUrl" character varying(500),
    "RegistrationStartDate" timestamp with time zone,
    "RegistrationEndDate" timestamp with time zone,
    "MaxTeams" integer DEFAULT 0 NOT NULL
);


--
-- Name: FinalResults; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."FinalResults" (
    "Id" text NOT NULL,
    "TeamId" character varying(64) NOT NULL,
    "RoundId" text,
    "EventId" text,
    "TrackId" text,
    "FinalScore" numeric(18,2) NOT NULL,
    "Rank" integer NOT NULL,
    "IsAdvanced" boolean NOT NULL,
    "CreatedBy" text,
    "LastUpdatedBy" text,
    "DeletedBy" text,
    "CreatedTime" timestamp with time zone NOT NULL,
    "LastUpdatedTime" timestamp with time zone NOT NULL,
    "DeletedTime" timestamp with time zone,
    "IsPublished" boolean DEFAULT false NOT NULL,
    "PrizeId" text
);


--
-- Name: Prizes; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."Prizes" (
    "Id" text NOT NULL,
    "EventId" text NOT NULL,
    "PrizeName" character varying(255) NOT NULL,
    "Value" character varying(500) DEFAULT ''::character varying NOT NULL,
    "Quantity" integer DEFAULT 1 NOT NULL,
    "CreatedTime" timestamp with time zone NOT NULL,
    "LastUpdatedTime" timestamp with time zone NOT NULL,
    "CreatedBy" text,
    "LastUpdatedBy" text,
    "IsDeleted" boolean DEFAULT false NOT NULL,
    "DeletedTime" timestamp with time zone,
    "DeletedBy" text
);


--
-- Name: Rounds; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."Rounds" (
    "Id" text NOT NULL,
    "EventId" text NOT NULL,
    "RoundName" character varying(255) NOT NULL,
    "RoundNumber" integer NOT NULL,
    "StartDate" timestamp with time zone NOT NULL,
    "EndDate" timestamp with time zone NOT NULL,
    "AdvancementRule" character varying(1000),
    "CreatedBy" text,
    "LastUpdatedBy" text,
    "DeletedBy" text,
    "CreatedTime" timestamp with time zone NOT NULL,
    "LastUpdatedTime" timestamp with time zone NOT NULL,
    "DeletedTime" timestamp with time zone,
    "ScoringStartDate" timestamp with time zone,
    "ScoringEndDate" timestamp with time zone,
    "AppealStartDate" timestamp with time zone,
    "AppealEndDate" timestamp with time zone
);


--
-- Name: Schools; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."Schools" (
    "Id" text NOT NULL,
    "SchoolName" character varying(255) NOT NULL,
    "Address" character varying(500),
    "CreatedBy" text,
    "LastUpdatedBy" text,
    "DeletedBy" text,
    "CreatedTime" timestamp with time zone NOT NULL,
    "LastUpdatedTime" timestamp with time zone NOT NULL,
    "DeletedTime" timestamp with time zone
);


--
-- Name: ScoreDetails; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."ScoreDetails" (
    "Id" text NOT NULL,
    "ScoreId" text NOT NULL,
    "TemplateId" text NOT NULL,
    "CriteriaId" text NOT NULL,
    "Value" numeric(18,2) NOT NULL,
    "CreatedBy" text,
    "LastUpdatedBy" text,
    "DeletedBy" text,
    "CreatedTime" timestamp with time zone NOT NULL,
    "LastUpdatedTime" timestamp with time zone NOT NULL,
    "DeletedTime" timestamp with time zone
);


--
-- Name: Scores; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."Scores" (
    "Id" text NOT NULL,
    "EventRoleId" text NOT NULL,
    "SubmitResultId" character varying(64) NOT NULL,
    "TotalScore" numeric(18,2) NOT NULL,
    "Comment" character varying(1000),
    "CreatedBy" text,
    "LastUpdatedBy" text,
    "DeletedBy" text,
    "CreatedTime" timestamp with time zone NOT NULL,
    "LastUpdatedTime" timestamp with time zone NOT NULL,
    "DeletedTime" timestamp with time zone,
    "IsSubmitted" boolean DEFAULT false NOT NULL
);


--
-- Name: SubmitResults; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."SubmitResults" (
    "Id" text NOT NULL,
    "TeamId" text NOT NULL,
    "TrackId" text NOT NULL,
    "SubmissionUrl" character varying(2000) NOT NULL,
    "Description" character varying(1000),
    "IsActive" boolean NOT NULL,
    "CreatedBy" text,
    "LastUpdatedBy" text,
    "DeletedBy" text,
    "CreatedTime" timestamp with time zone NOT NULL,
    "LastUpdatedTime" timestamp with time zone NOT NULL,
    "DeletedTime" timestamp with time zone
);


--
-- Name: TeamInvitations; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."TeamInvitations" (
    "Id" text NOT NULL,
    "TeamId" text NOT NULL,
    "InvitedUserId" text NOT NULL,
    "InvitedByUserId" text NOT NULL,
    "Status" character varying(30) NOT NULL,
    "ExpiresAt" timestamp with time zone NOT NULL,
    "RespondedAt" timestamp with time zone,
    "Notes" character varying(500),
    "CreatedBy" text,
    "LastUpdatedBy" text,
    "DeletedBy" text,
    "CreatedTime" timestamp with time zone NOT NULL,
    "LastUpdatedTime" timestamp with time zone NOT NULL,
    "DeletedTime" timestamp with time zone
);


--
-- Name: Teams; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."Teams" (
    "Id" text NOT NULL,
    "EventId" text NOT NULL,
    "Name" character varying(150) NOT NULL,
    "Description" character varying(500),
    "IsActive" boolean NOT NULL,
    "Status" integer NOT NULL,
    "CreatedBy" text,
    "LastUpdatedBy" text,
    "DeletedBy" text,
    "CreatedTime" timestamp with time zone NOT NULL,
    "LastUpdatedTime" timestamp with time zone NOT NULL,
    "DeletedTime" timestamp with time zone,
    "LastRejectReason" text
);


--
-- Name: TemplateCriterias; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."TemplateCriterias" (
    "TemplateId" text NOT NULL,
    "CriteriaId" text NOT NULL,
    "Weight" numeric(18,2) NOT NULL,
    "MaxScore" numeric(18,2) NOT NULL
);


--
-- Name: Templates; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."Templates" (
    "Id" text NOT NULL,
    "TemplateName" character varying(255) NOT NULL,
    "Description" character varying(1000),
    "CreatedBy" text,
    "LastUpdatedBy" text,
    "DeletedBy" text,
    "CreatedTime" timestamp with time zone NOT NULL,
    "LastUpdatedTime" timestamp with time zone NOT NULL,
    "DeletedTime" timestamp with time zone
);


--
-- Name: Tracks; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."Tracks" (
    "Id" text NOT NULL,
    "RoundId" text NOT NULL,
    "TemplateId" text,
    "TrackName" character varying(255) NOT NULL,
    "Description" character varying(1000),
    "SubmissionRuleDescription" character varying(2000),
    "CreatedBy" text,
    "LastUpdatedBy" text,
    "DeletedBy" text,
    "CreatedTime" timestamp with time zone NOT NULL,
    "LastUpdatedTime" timestamp with time zone NOT NULL,
    "DeletedTime" timestamp with time zone,
    "StartDate" timestamp with time zone,
    "EndDate" timestamp with time zone,
    "ScoringStartDate" timestamp with time zone,
    "ScoringEndDate" timestamp with time zone
);


--
-- Name: UserRejections; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."UserRejections" (
    "Id" text NOT NULL,
    "UserId" text NOT NULL,
    "RejectedBy" text NOT NULL,
    "Reason" character varying(1000),
    "CreatedBy" text,
    "LastUpdatedBy" text,
    "DeletedBy" text,
    "CreatedTime" timestamp with time zone NOT NULL,
    "LastUpdatedTime" timestamp with time zone NOT NULL,
    "DeletedTime" timestamp with time zone,
    "IsActive" boolean DEFAULT false NOT NULL
);


--
-- Name: Users; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."Users" (
    "Id" text NOT NULL,
    "SchoolId" text,
    "StudentCode" character varying(50),
    "Email" character varying(255) NOT NULL,
    "PasswordHash" text NOT NULL,
    "FullName" character varying(255) NOT NULL,
    "IsStudent" boolean NOT NULL,
    "IsAdmin" boolean NOT NULL,
    "IsApproved" boolean NOT NULL,
    "RefreshToken" text,
    "RefreshTokenExpiryTime" timestamp with time zone,
    "IsEmailVerified" boolean NOT NULL,
    "EmailVerificationToken" text,
    "EmailVerificationExpiry" timestamp with time zone,
    "IsFpt" boolean DEFAULT true NOT NULL,
    "PhotoStudentCardUrl" character varying(500),
    "IsTemporary" boolean NOT NULL,
    "CreatedBy" text,
    "LastUpdatedBy" text,
    "DeletedBy" text,
    "CreatedTime" timestamp with time zone NOT NULL,
    "LastUpdatedTime" timestamp with time zone NOT NULL,
    "DeletedTime" timestamp with time zone
);


--
-- Name: __EFMigrationsHistory; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL
);


--
-- Name: Appeals PK_Appeals; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Appeals"
    ADD CONSTRAINT "PK_Appeals" PRIMARY KEY ("Id");


--
-- Name: Criterias PK_Criterias; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Criterias"
    ADD CONSTRAINT "PK_Criterias" PRIMARY KEY ("Id");


--
-- Name: EventRoleInvitations PK_EventRoleInvitations; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."EventRoleInvitations"
    ADD CONSTRAINT "PK_EventRoleInvitations" PRIMARY KEY ("Id");


--
-- Name: EventRoles PK_EventRoles; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."EventRoles"
    ADD CONSTRAINT "PK_EventRoles" PRIMARY KEY ("Id");


--
-- Name: Events PK_Events; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Events"
    ADD CONSTRAINT "PK_Events" PRIMARY KEY ("Id");


--
-- Name: FinalResults PK_FinalResults; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."FinalResults"
    ADD CONSTRAINT "PK_FinalResults" PRIMARY KEY ("Id");


--
-- Name: Prizes PK_Prizes; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Prizes"
    ADD CONSTRAINT "PK_Prizes" PRIMARY KEY ("Id");


--
-- Name: Rounds PK_Rounds; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Rounds"
    ADD CONSTRAINT "PK_Rounds" PRIMARY KEY ("Id");


--
-- Name: Schools PK_Schools; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Schools"
    ADD CONSTRAINT "PK_Schools" PRIMARY KEY ("Id");


--
-- Name: ScoreDetails PK_ScoreDetails; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."ScoreDetails"
    ADD CONSTRAINT "PK_ScoreDetails" PRIMARY KEY ("Id");


--
-- Name: Scores PK_Scores; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Scores"
    ADD CONSTRAINT "PK_Scores" PRIMARY KEY ("Id");


--
-- Name: SubmitResults PK_SubmitResults; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."SubmitResults"
    ADD CONSTRAINT "PK_SubmitResults" PRIMARY KEY ("Id");


--
-- Name: TeamInvitations PK_TeamInvitations; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."TeamInvitations"
    ADD CONSTRAINT "PK_TeamInvitations" PRIMARY KEY ("Id");


--
-- Name: Teams PK_Teams; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Teams"
    ADD CONSTRAINT "PK_Teams" PRIMARY KEY ("Id");


--
-- Name: TemplateCriterias PK_TemplateCriterias; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."TemplateCriterias"
    ADD CONSTRAINT "PK_TemplateCriterias" PRIMARY KEY ("TemplateId", "CriteriaId");


--
-- Name: Templates PK_Templates; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Templates"
    ADD CONSTRAINT "PK_Templates" PRIMARY KEY ("Id");


--
-- Name: Tracks PK_Tracks; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Tracks"
    ADD CONSTRAINT "PK_Tracks" PRIMARY KEY ("Id");


--
-- Name: UserRejections PK_UserRejections; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."UserRejections"
    ADD CONSTRAINT "PK_UserRejections" PRIMARY KEY ("Id");


--
-- Name: Users PK_Users; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Users"
    ADD CONSTRAINT "PK_Users" PRIMARY KEY ("Id");


--
-- Name: __EFMigrationsHistory PK___EFMigrationsHistory; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."__EFMigrationsHistory"
    ADD CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId");


--
-- Name: IX_Appeals_SubmitResultId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Appeals_SubmitResultId" ON public."Appeals" USING btree ("SubmitResultId");


--
-- Name: IX_Appeals_TeamId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Appeals_TeamId" ON public."Appeals" USING btree ("TeamId");


--
-- Name: IX_EventRoleInvitations_EventId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_EventRoleInvitations_EventId" ON public."EventRoleInvitations" USING btree ("EventId");


--
-- Name: IX_EventRoleInvitations_InvitedUserId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_EventRoleInvitations_InvitedUserId" ON public."EventRoleInvitations" USING btree ("InvitedUserId");


--
-- Name: IX_EventRoleInvitations_Token; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_EventRoleInvitations_Token" ON public."EventRoleInvitations" USING btree ("Token");


--
-- Name: IX_EventRoleInvitations_TrackId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_EventRoleInvitations_TrackId" ON public."EventRoleInvitations" USING btree ("TrackId");


--
-- Name: IX_EventRoles_EventId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_EventRoles_EventId" ON public."EventRoles" USING btree ("EventId");


--
-- Name: IX_EventRoles_Status; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_EventRoles_Status" ON public."EventRoles" USING btree ("Status");


--
-- Name: IX_EventRoles_TeamId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_EventRoles_TeamId" ON public."EventRoles" USING btree ("TeamId");


--
-- Name: IX_EventRoles_TrackId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_EventRoles_TrackId" ON public."EventRoles" USING btree ("TrackId");


--
-- Name: IX_EventRoles_UserId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_EventRoles_UserId" ON public."EventRoles" USING btree ("UserId");


--
-- Name: IX_FinalResults_EventId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_FinalResults_EventId" ON public."FinalResults" USING btree ("EventId");


--
-- Name: IX_FinalResults_PrizeId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_FinalResults_PrizeId" ON public."FinalResults" USING btree ("PrizeId");


--
-- Name: IX_FinalResults_RoundId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_FinalResults_RoundId" ON public."FinalResults" USING btree ("RoundId");


--
-- Name: IX_FinalResults_TeamId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_FinalResults_TeamId" ON public."FinalResults" USING btree ("TeamId");


--
-- Name: IX_FinalResults_TrackId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_FinalResults_TrackId" ON public."FinalResults" USING btree ("TrackId");


--
-- Name: IX_Prizes_EventId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Prizes_EventId" ON public."Prizes" USING btree ("EventId");


--
-- Name: IX_Rounds_EventId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Rounds_EventId" ON public."Rounds" USING btree ("EventId");


--
-- Name: IX_ScoreDetails_ScoreId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_ScoreDetails_ScoreId" ON public."ScoreDetails" USING btree ("ScoreId");


--
-- Name: IX_ScoreDetails_TemplateId_CriteriaId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_ScoreDetails_TemplateId_CriteriaId" ON public."ScoreDetails" USING btree ("TemplateId", "CriteriaId");


--
-- Name: IX_Scores_EventRoleId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Scores_EventRoleId" ON public."Scores" USING btree ("EventRoleId");


--
-- Name: IX_Scores_EventRoleId_SubmitResultId; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_Scores_EventRoleId_SubmitResultId" ON public."Scores" USING btree ("EventRoleId", "SubmitResultId");


--
-- Name: IX_Scores_SubmitResultId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Scores_SubmitResultId" ON public."Scores" USING btree ("SubmitResultId");


--
-- Name: IX_SubmitResults_TeamId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_SubmitResults_TeamId" ON public."SubmitResults" USING btree ("TeamId");


--
-- Name: IX_SubmitResults_TrackId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_SubmitResults_TrackId" ON public."SubmitResults" USING btree ("TrackId");


--
-- Name: IX_TeamInvitations_InvitedUserId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_TeamInvitations_InvitedUserId" ON public."TeamInvitations" USING btree ("InvitedUserId");


--
-- Name: IX_TeamInvitations_TeamId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_TeamInvitations_TeamId" ON public."TeamInvitations" USING btree ("TeamId");


--
-- Name: IX_Teams_EventId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Teams_EventId" ON public."Teams" USING btree ("EventId");


--
-- Name: IX_TemplateCriterias_CriteriaId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_TemplateCriterias_CriteriaId" ON public."TemplateCriterias" USING btree ("CriteriaId");


--
-- Name: IX_Tracks_RoundId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Tracks_RoundId" ON public."Tracks" USING btree ("RoundId");


--
-- Name: IX_Tracks_TemplateId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Tracks_TemplateId" ON public."Tracks" USING btree ("TemplateId");


--
-- Name: IX_UserRejections_UserId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_UserRejections_UserId" ON public."UserRejections" USING btree ("UserId");


--
-- Name: IX_Users_Email; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_Users_Email" ON public."Users" USING btree ("Email");


--
-- Name: IX_Users_SchoolId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Users_SchoolId" ON public."Users" USING btree ("SchoolId");


--
-- Name: Appeals FK_Appeals_SubmitResults_SubmitResultId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Appeals"
    ADD CONSTRAINT "FK_Appeals_SubmitResults_SubmitResultId" FOREIGN KEY ("SubmitResultId") REFERENCES public."SubmitResults"("Id") ON DELETE CASCADE;


--
-- Name: Appeals FK_Appeals_Teams_TeamId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Appeals"
    ADD CONSTRAINT "FK_Appeals_Teams_TeamId" FOREIGN KEY ("TeamId") REFERENCES public."Teams"("Id") ON DELETE RESTRICT;


--
-- Name: EventRoleInvitations FK_EventRoleInvitations_Events_EventId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."EventRoleInvitations"
    ADD CONSTRAINT "FK_EventRoleInvitations_Events_EventId" FOREIGN KEY ("EventId") REFERENCES public."Events"("Id") ON DELETE CASCADE;


--
-- Name: EventRoleInvitations FK_EventRoleInvitations_Tracks_TrackId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."EventRoleInvitations"
    ADD CONSTRAINT "FK_EventRoleInvitations_Tracks_TrackId" FOREIGN KEY ("TrackId") REFERENCES public."Tracks"("Id") ON DELETE RESTRICT;


--
-- Name: EventRoleInvitations FK_EventRoleInvitations_Users_InvitedUserId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."EventRoleInvitations"
    ADD CONSTRAINT "FK_EventRoleInvitations_Users_InvitedUserId" FOREIGN KEY ("InvitedUserId") REFERENCES public."Users"("Id") ON DELETE RESTRICT;


--
-- Name: EventRoles FK_EventRoles_Events_EventId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."EventRoles"
    ADD CONSTRAINT "FK_EventRoles_Events_EventId" FOREIGN KEY ("EventId") REFERENCES public."Events"("Id") ON DELETE CASCADE;


--
-- Name: EventRoles FK_EventRoles_Teams_TeamId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."EventRoles"
    ADD CONSTRAINT "FK_EventRoles_Teams_TeamId" FOREIGN KEY ("TeamId") REFERENCES public."Teams"("Id");


--
-- Name: EventRoles FK_EventRoles_Tracks_TrackId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."EventRoles"
    ADD CONSTRAINT "FK_EventRoles_Tracks_TrackId" FOREIGN KEY ("TrackId") REFERENCES public."Tracks"("Id") ON DELETE RESTRICT;


--
-- Name: EventRoles FK_EventRoles_Users_UserId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."EventRoles"
    ADD CONSTRAINT "FK_EventRoles_Users_UserId" FOREIGN KEY ("UserId") REFERENCES public."Users"("Id") ON DELETE CASCADE;


--
-- Name: FinalResults FK_FinalResults_Events_EventId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."FinalResults"
    ADD CONSTRAINT "FK_FinalResults_Events_EventId" FOREIGN KEY ("EventId") REFERENCES public."Events"("Id") ON DELETE RESTRICT;


--
-- Name: FinalResults FK_FinalResults_Prizes_PrizeId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."FinalResults"
    ADD CONSTRAINT "FK_FinalResults_Prizes_PrizeId" FOREIGN KEY ("PrizeId") REFERENCES public."Prizes"("Id") ON DELETE SET NULL;


--
-- Name: FinalResults FK_FinalResults_Rounds_RoundId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."FinalResults"
    ADD CONSTRAINT "FK_FinalResults_Rounds_RoundId" FOREIGN KEY ("RoundId") REFERENCES public."Rounds"("Id") ON DELETE RESTRICT;


--
-- Name: FinalResults FK_FinalResults_Teams_TeamId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."FinalResults"
    ADD CONSTRAINT "FK_FinalResults_Teams_TeamId" FOREIGN KEY ("TeamId") REFERENCES public."Teams"("Id") ON DELETE RESTRICT;


--
-- Name: FinalResults FK_FinalResults_Tracks_TrackId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."FinalResults"
    ADD CONSTRAINT "FK_FinalResults_Tracks_TrackId" FOREIGN KEY ("TrackId") REFERENCES public."Tracks"("Id") ON DELETE RESTRICT;


--
-- Name: Prizes FK_Prizes_Events_EventId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Prizes"
    ADD CONSTRAINT "FK_Prizes_Events_EventId" FOREIGN KEY ("EventId") REFERENCES public."Events"("Id") ON DELETE CASCADE;


--
-- Name: Rounds FK_Rounds_Events_EventId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Rounds"
    ADD CONSTRAINT "FK_Rounds_Events_EventId" FOREIGN KEY ("EventId") REFERENCES public."Events"("Id") ON DELETE CASCADE;


--
-- Name: ScoreDetails FK_ScoreDetails_Scores_ScoreId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."ScoreDetails"
    ADD CONSTRAINT "FK_ScoreDetails_Scores_ScoreId" FOREIGN KEY ("ScoreId") REFERENCES public."Scores"("Id") ON DELETE CASCADE;


--
-- Name: ScoreDetails FK_ScoreDetails_TemplateCriterias_TemplateId_CriteriaId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."ScoreDetails"
    ADD CONSTRAINT "FK_ScoreDetails_TemplateCriterias_TemplateId_CriteriaId" FOREIGN KEY ("TemplateId", "CriteriaId") REFERENCES public."TemplateCriterias"("TemplateId", "CriteriaId") ON DELETE RESTRICT;


--
-- Name: Scores FK_Scores_EventRoles_EventRoleId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Scores"
    ADD CONSTRAINT "FK_Scores_EventRoles_EventRoleId" FOREIGN KEY ("EventRoleId") REFERENCES public."EventRoles"("Id") ON DELETE CASCADE;


--
-- Name: Scores FK_Scores_SubmitResults_SubmitResultId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Scores"
    ADD CONSTRAINT "FK_Scores_SubmitResults_SubmitResultId" FOREIGN KEY ("SubmitResultId") REFERENCES public."SubmitResults"("Id") ON DELETE RESTRICT;


--
-- Name: SubmitResults FK_SubmitResults_Teams_TeamId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."SubmitResults"
    ADD CONSTRAINT "FK_SubmitResults_Teams_TeamId" FOREIGN KEY ("TeamId") REFERENCES public."Teams"("Id") ON DELETE CASCADE;


--
-- Name: SubmitResults FK_SubmitResults_Tracks_TrackId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."SubmitResults"
    ADD CONSTRAINT "FK_SubmitResults_Tracks_TrackId" FOREIGN KEY ("TrackId") REFERENCES public."Tracks"("Id") ON DELETE CASCADE;


--
-- Name: TeamInvitations FK_TeamInvitations_Teams_TeamId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."TeamInvitations"
    ADD CONSTRAINT "FK_TeamInvitations_Teams_TeamId" FOREIGN KEY ("TeamId") REFERENCES public."Teams"("Id") ON DELETE CASCADE;


--
-- Name: TeamInvitations FK_TeamInvitations_Users_InvitedUserId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."TeamInvitations"
    ADD CONSTRAINT "FK_TeamInvitations_Users_InvitedUserId" FOREIGN KEY ("InvitedUserId") REFERENCES public."Users"("Id") ON DELETE RESTRICT;


--
-- Name: Teams FK_Teams_Events_EventId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Teams"
    ADD CONSTRAINT "FK_Teams_Events_EventId" FOREIGN KEY ("EventId") REFERENCES public."Events"("Id") ON DELETE CASCADE;


--
-- Name: TemplateCriterias FK_TemplateCriterias_Criterias_CriteriaId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."TemplateCriterias"
    ADD CONSTRAINT "FK_TemplateCriterias_Criterias_CriteriaId" FOREIGN KEY ("CriteriaId") REFERENCES public."Criterias"("Id") ON DELETE CASCADE;


--
-- Name: TemplateCriterias FK_TemplateCriterias_Templates_TemplateId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."TemplateCriterias"
    ADD CONSTRAINT "FK_TemplateCriterias_Templates_TemplateId" FOREIGN KEY ("TemplateId") REFERENCES public."Templates"("Id") ON DELETE CASCADE;


--
-- Name: Tracks FK_Tracks_Rounds_RoundId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Tracks"
    ADD CONSTRAINT "FK_Tracks_Rounds_RoundId" FOREIGN KEY ("RoundId") REFERENCES public."Rounds"("Id") ON DELETE CASCADE;


--
-- Name: Tracks FK_Tracks_Templates_TemplateId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Tracks"
    ADD CONSTRAINT "FK_Tracks_Templates_TemplateId" FOREIGN KEY ("TemplateId") REFERENCES public."Templates"("Id") ON DELETE SET NULL;


--
-- Name: UserRejections FK_UserRejections_Users_UserId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."UserRejections"
    ADD CONSTRAINT "FK_UserRejections_Users_UserId" FOREIGN KEY ("UserId") REFERENCES public."Users"("Id") ON DELETE CASCADE;


--
-- Name: Users FK_Users_Schools_SchoolId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Users"
    ADD CONSTRAINT "FK_Users_Schools_SchoolId" FOREIGN KEY ("SchoolId") REFERENCES public."Schools"("Id") ON DELETE RESTRICT;


--
-- PostgreSQL database dump complete
--


