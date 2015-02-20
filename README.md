SyncOMatic
==========

![Icon](https://raw.github.com/Particular/SyncOMatic/master/Icons/package_icon.png)

A tool to help synchronizing specific files and folders across repositories

### Run with a new repository

Use our friendly helper [pbot](https://github.com/Particular/PBot).

1. `pbot add repo reponame`
2. `pbot sync reponame target branch branchname`
3. If ok, a pull request will be created for you

### Run on an existing repository
1. `sync reponame target branch branchname`
2. If ok, a pull request will be created for you

### Update/add more files to sync

* Extend the [default repository template](https://github.com/Particular/PBot/blob/master/src/PBot/SyncOMatic/DefaultTemplateRepo.cs) inside the [pbot repo](https://github.com/Particular/PBot)

Happy syncing!

## Troubleshooting

`pbot help` or `pbot help sync`

## Icon 

<a href="http://thenounproject.com/term/sync/290/" target="_blank">Sync</a> designed by <a href="http://www.thenounproject.com/edward" target="_blank">Edward Boatman</a> from The Noun Project


