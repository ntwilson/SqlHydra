﻿module Oracle.Generation

open Expecto
open VerifyTests
open VerifyExpecto
open SqlHydra
open SqlHydra.Oracle
open SqlHydra.Domain

let cfg = 
    {
        ConnectionString = DB.connectionString
        OutputFile = ""
        Namespace = "TestNS"
        IsCLIMutable = true
        Readers = Some { ReadersConfig.ReaderType = "Oracle.ManagedDataAccess.Client.OracleDataReader" } 
        Filters = { Includes = [ "OT/*" ]; Excludes = [ ] }
    }

[<Tests>]
let tests = 
    categoryList "Oracle" "Generation Integration Tests" [

        ptest "Print Schema" {
            let schema = OracleSchemaProvider.getSchema cfg
            printfn "Schema: %A" schema
        }

        let lazySchema = lazy OracleSchemaProvider.getSchema cfg

        let getCode cfg = 
            lazySchema.Value
            |> SchemaGenerator.generateModule cfg SqlHydra.Oracle.Program.app
            |> SchemaGenerator.toFormattedCode cfg SqlHydra.Oracle.Program.app

        let inCode (str: string) cfg = 
            let code = getCode cfg
            Expect.isTrue (code.Contains str) ""

        let notInCode (str: string) cfg = 
            let code = getCode cfg
            Expect.isFalse (code.Contains str) ""

        testTask "Verify Generated Code"  {
            let code = getCode cfg
            
            let settings = VerifySettings()
            settings.UseDirectory("./Verify")
            settings.ScrubLines(fun line -> line.StartsWith("// This code was generated by `SqlHydra.Oracle`"))
            VerifierSettings.OmitContentFromException() // Cleans up FAKE build output
#if NET5_0
            do! Verifier.Verify("Verify Generated Code NET5", code, settings)
#endif
#if NET6_0
            do! Verifier.Verify("Verify Generated Code NET6", code, settings)
#endif
        }
    
        test "Code Should Have Reader"  {
            cfg |> inCode "type HydraReader"
        }
    
        test "Code Should Not Have Reader"  {
            { cfg with Readers = None } |> notInCode "type HydraReader"
        }

        test "Code Should Have CLIMutable"  {
            { cfg with IsCLIMutable = true } |> inCode "[<CLIMutable>]"
        }

        test "Code Should Not Have CLIMutable"  {
            { cfg with IsCLIMutable = false } |> notInCode "[<CLIMutable>]"
        }

        test "Code Should Have Namespace" {
            cfg |> inCode "namespace TestNS"
        }

    ]