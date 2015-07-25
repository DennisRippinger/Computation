CREATE DATABASE `Computation` DEFAULT CHARACTER SET utf8 COLLATE utf8_general_ci;
USE `Computation`;


CREATE TABLE IF NOT EXISTS `OpenTicketsView` (
`Id` int(11)
,`Group_Id` int(11)
,`BallotBox` varchar(255)
,`Number` int(11)

);
CREATE TABLE IF NOT EXISTS `SecondVoteView` (
`Lfdnr` int(11)
,`Name` varchar(255)
,`List` varchar(255)
,`Faculty` varchar(255)
);

CREATE TABLE IF NOT EXISTS `ballotBoxes` (
  `id` int(11) NOT NULL,
  `name` varchar(255) NOT NULL,
  PRIMARY KEY  (`id`)
) ENGINE=MyISAM DEFAULT CHARSET=utf8;

CREATE TABLE IF NOT EXISTS `candidates` (
  `lfdnr` int(11) NOT NULL,
  `name` varchar(255) NOT NULL,
  `list_lfdnr` int(11) NOT NULL,
  `faculty` int(11) NOT NULL
) ENGINE=MyISAM DEFAULT CHARSET=utf8;

CREATE TABLE IF NOT EXISTS `faculties` (
  `ID` int(11) NOT NULL auto_increment,
  `name` varchar(255) NOT NULL,
  PRIMARY KEY  (`ID`)
) ENGINE=MyISAM  DEFAULT CHARSET=utf8 AUTO_INCREMENT=12 ;

CREATE TABLE IF NOT EXISTS `firstVotes` (
  `ID` int(11) NOT NULL auto_increment,
  `ldfnr` int(11) NOT NULL,
  `ballotBox` int(11) NOT NULL,
  `timestamp` timestamp NOT NULL default CURRENT_TIMESTAMP on update CURRENT_TIMESTAMP,
  PRIMARY KEY  (`ID`)
) ENGINE=MyISAM  DEFAULT CHARSET=utf8 AUTO_INCREMENT=45 ;

CREATE TABLE IF NOT EXISTS `lists` (
  `ldfnr` int(11) NOT NULL,
  `list` varchar(255) NOT NULL
) ENGINE=MyISAM DEFAULT CHARSET=utf8;

CREATE TABLE IF NOT EXISTS `secondVotes` (
  `ID` int(11) NOT NULL auto_increment,
  `ldfnr` int(11) NOT NULL,
  `ballotBox` int(11) NOT NULL,
  `timestamp` timestamp NOT NULL default CURRENT_TIMESTAMP on update CURRENT_TIMESTAMP,
  PRIMARY KEY  (`ID`)
) ENGINE=MyISAM  DEFAULT CHARSET=utf8 AUTO_INCREMENT=45 ;

CREATE TABLE IF NOT EXISTS `tickets` (
  `id` int(11) NOT NULL auto_increment,
  `open` tinyint(1) NOT NULL,
  `group_id` int(11) NOT NULL,
  `ballotBox` int(11) NOT NULL,
  `number` int(11) NOT NULL,
  PRIMARY KEY  (`id`)
) ENGINE=MyISAM  DEFAULT CHARSET=utf8 AUTO_INCREMENT=20 ;

DROP TABLE IF EXISTS `OpenTicketsView`;

CREATE ALGORITHM=MERGE DEFINER=`root`@`localhost` SQL SECURITY DEFINER VIEW `OpenTicketsView` AS select `tickets`.`id` AS `Id`,`tickets`.`group_id` AS `Group_Id`,`ballotBoxes`.`name` AS `BallotBox`,`tickets`.`number` AS `Number` from (`tickets` join `ballotBoxes` on((`tickets`.`ballotBox` = `ballotBoxes`.`id`))) where (`tickets`.`open` = 1);

DROP TABLE IF EXISTS `SecondVoteView`;

CREATE ALGORITHM=MERGE DEFINER=`root`@`localhost` SQL SECURITY DEFINER VIEW `SecondVoteView` AS select `candidates`.`lfdnr` AS `Lfdnr`,`candidates`.`name` AS `Name`,`lists`.`list` AS `List`,`faculties`.`name` AS `Faculty` from ((`candidates` join `lists` on((`lists`.`ldfnr` = `candidates`.`list_lfdnr`))) join `faculties` on((`faculties`.`ID` = `candidates`.`faculty`)));
